#include "Mesh.h"
#include <rpp/file_io.h>
#include <cassert>
#include <unordered_set>

namespace mesh
{
    ///////////////////////////////////////////////////////////////////////////////////////////////

    static bool SaveMaterials(const Mesh& mesh, const string& filename) noexcept
    {
        if (mesh.Groups.empty())
            return false;
        if ([&]{ for (const MeshGroup& group : mesh.Groups) if (group.Mat) return false; return true; }())
            return false;

        vector<Material*> written;
        shared_ptr<Material> defaultMat;
        auto getDefaultMat = [&]
        {
            if (defaultMat)
                return defaultMat;
            defaultMat = mesh.FindMaterial("default"_sv);
            if (!defaultMat) {
                defaultMat = make_shared<Material>();
                defaultMat->Name = "default"s;
            }
            return defaultMat;
        };

        if (file f = file{ filename, CREATENEW })
        {
            auto writeColor = [&](strview id, Color3 color) { f.writeln(id, color.r, color.g, color.b); };
            auto writeStr   = [&](strview id, strview str)  { if (str) f.writeln(id, str); };
            auto writeFloat = [&](strview id, float value)  { if (value != 1.0f) f.writeln(id, value); };

            f.writeln("#", filename, "MTL library");
            for (const MeshGroup& group : mesh.Groups)
            {
                Material& mat = *(group.Mat ? group.Mat : getDefaultMat()).get();
                if (contains(written, &mat))
                    continue; // skip
                written.push_back(&mat);

                f.writeln("newmtl", mat.Name);
                writeColor("Ka", mat.AmbientColor);
                writeColor("Kd", mat.DiffuseColor);
                writeColor("Ks", mat.SpecularColor);
                if (mat.EmissiveColor.notZero())
                    writeColor("Ke", mat.EmissiveColor);

                writeFloat("Ns", clamp(mat.Specular*1000.0f, 0.0f, 1000.0f)); // Ns is [0, 1000]
                writeFloat("d", mat.Alpha);

                writeStr("map_Kd",   mat.DiffusePath);
                writeStr("map_d",    mat.AlphaPath);
                writeStr("map_Ks",   mat.SpecularPath);
                writeStr("map_bump", mat.NormalPath);
                writeStr("map_Ke",   mat.EmissivePath);

                f.writeln("illum 2"); // default smooth shaded rendering
                f.writeln();
            }
            return true;
        }
        fprintf(stderr, "Failed to create file '%s'\n", filename.c_str());
        return false;
    }

    static vector<shared_ptr<Material>> LoadMaterials(const string& matlibFile)
    {
        vector<shared_ptr<Material>> materials;

        if (auto parser = buffer_line_parser::from_file(matlibFile))
        {
            Material* mat = nullptr;
            strview line;
            while (parser.read_line(line))
            {
                strview id = line.next(' ');
                if (id == "newmtl")
                {
                    materials.push_back(make_shared<Material>());
                    mat = materials.back().get();
                    mat->Name = (string)line.trim();
                }
                else if (mat)
                {
                    if      (id == "Ka") mat->AmbientColor  = Color3::parseColor(line);
                    else if (id == "Kd") mat->DiffuseColor  = Color3::parseColor(line);
                    else if (id == "Ks") mat->SpecularColor = Color3::parseColor(line);
                    else if (id == "Ke") mat->EmissiveColor = Color3::parseColor(line);
                    else if (id == "Ns") mat->Specular = line.to_float() / 1000.0f; // Ns is [0, 1000], normalize to [0, 1]
                    else if (id == "d")  mat->Alpha    = line.to_float();
                    else if (id == "Tr") mat->Alpha    = 1.0f - line.to_float();
                    else if (id == "map_Kd")   mat->DiffusePath  = line.next(' ');
                    else if (id == "map_d")    mat->AlphaPath    = line.next(' ');
                    else if (id == "map_Ks")   mat->SpecularPath = line.next(' ');
                    else if (id == "map_bump") mat->NormalPath   = line.next(' ');
                    else if (id == "map_Ke")   mat->EmissivePath = line.next(' ');
                }
            }
        }
        return materials;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    static constexpr size_t MaxStackAlloc = 1024 * 1024;

    struct ObjLoader
    {
        Mesh& mesh;
        const string& meshPath;
        buffer_line_parser parser;
        size_t numVerts = 0, numCoords = 0, numNormals = 0, numFaces = 0;
        vector<shared_ptr<Material>> materials;
        MeshGroup* group = nullptr;
        bool triedDefaultMat = false;

        Vector3* vertsData   = nullptr;
        Vector2* coordsData  = nullptr;
        Vector3* normalsData = nullptr;
        Color3*  colorsData  = nullptr;
        int* vertsLayer   = nullptr;
        int* coordsLayer  = nullptr;
        int* normalsLayer = nullptr;
        int* colorsLayer  = nullptr;
        int* layersEnd = nullptr;

        void* dataBuffer = nullptr;
        size_t bufferSize = 0;

        explicit ObjLoader(Mesh& mesh, const string& meshPath)
            : mesh{ mesh }, meshPath { meshPath }, parser{ buffer_line_parser::from_file(meshPath) }
        {
        }

        ~ObjLoader()
        {
            if (bufferSize > MaxStackAlloc) free(dataBuffer);
        }

        bool ProbeStats()
        {
            strview line;
            while (parser.read_line(line))
            {
                char c = line[0];
                if (c == 'v') switch (line[1])
                {
                    case ' ': ++numVerts;   break;
                    case 'n': ++numNormals; break;
                    case 't': ++numCoords;  break;
                    default:;
                }
                else if (c == 'f' && line[1] == ' ')
                {
                    ++numFaces;
                }
            }

            parser.reset();
            if (numVerts == 0) {
                fprintf(stderr, "Mesh::LoadOBJ() failed: No vertices in %s\n", meshPath.c_str());
                return false;
            }

            // megaBuffer strategy - one big allocation instead of a dozen small ones
            bufferSize = numVerts    * sizeof(Vector3)
                        + numCoords  * sizeof(Vector2)
                        + numNormals * sizeof(Vector3)
                        + numVerts   * sizeof(Color3)
                        + numVerts   * sizeof(int) 
                        + numCoords  * sizeof(int) 
                        + numNormals * sizeof(int) 
                        + numVerts   * sizeof(int);
            return true;
        }

        struct pool_helper {
            void* ptr;
            template<class T> T* next(size_t count) {
                T* data = (T*)ptr;
                ptr = data + count;
                return data;
            }
        };

        void InitPointers(void* allocated)
        {
            dataBuffer = allocated;
            pool_helper pool = { (byte*) allocated };
            vertsData   = pool.next<Vector3>(numVerts);
            coordsData  = pool.next<Vector2>(numCoords);
            normalsData = pool.next<Vector3>(numNormals);
            colorsData  = pool.next<Color3>(numVerts);

            vertsLayer   = pool.next<int>(numVerts);
            coordsLayer  = pool.next<int>(numCoords);
            normalsLayer = pool.next<int>(numNormals);
            colorsLayer  = pool.next<int>(numVerts);
            layersEnd    = pool.next<int>(0);
        }

        shared_ptr<Material> FindMat(strview matName)
        {
            if (materials.empty() && !triedDefaultMat) {
                triedDefaultMat = true;
                materials = LoadMaterials(file_replace_ext(meshPath, "obj"));
            }
            for (auto& mat : materials)
                if (matName.equalsi(mat->Name))
                    return mat;
            return {};
        }

        MeshGroup* CurrentGroup()
        {
            return group ? group : (group = &mesh.CreateGroup({}));
        }

        void ParseMeshData()
        {
            int vertexId = 0, coordId = 0, normalId = 0, colorId = 0;

            strview line;
            while (parser.read_line(line)) // for each line
            {
                char c = line[0];
                if (c == 'v')
                {
                    c = line[1];
                    if (c == ' ') { // v 1.0 1.0 1.0
                        line.skip(2); // skip 'v '
                        Vector3& v = vertsData[vertexId];
                        line >> v.x >> v.y >> v.z;

                        if (!line.empty())
                        {
                            Vector3 col;
                            line >> col.x >> col.y >> col.z;
                            if (col.sqlength() > 0.001f)
                            {
                                // for OBJ we always use Per-Vertex color mapping
                                if (colorId == 0) {
                                    memset(colorsData, 0, numVerts*sizeof(Color3));
                                }
                                ++colorId;
                                colorsData[vertexId] = col;
                            }
                        }
                        // Use this if exporting for Direct3D
                        //v.z = -v.z; // invert Z to convert to lhs coordinates
                        ++vertexId;
                        continue;
                    }
                    if (c == 'n') { // vn 1.0 1.0 1.0
                        line.skip(3); // skip 'vn '
                        Vector3& n = normalsData[normalId++];
                        line >> n.x >> n.y >> n.z;
                        // Use this if exporting for Direct3D
                        //n.z = -n.z; // invert Z to convert to lhs coordinates
                        continue;
                    }
                    if (c == 't') { // vt 1.0 1.0
                        line.skip(3); // skip 'vt '
                        Vector2& uv = coordsData[coordId++];
                        line >> uv.x >> uv.y;
                        //if (fmt == TXC_Direct3DTexCoords) // Use this if exporting for Direct3D
                        //    c.y = 1.0f - c.y; // invert the V coord to convert to lhs coordinates
                        continue;
                    }
                }
                else if (c == 'f')
                {
                    // f Vertex1/Texture1/Normal1 Vertex2/Texture2/Normal2 Vertex3/Texture3/Normal3
                    auto& faces = CurrentGroup()->Faces;
                    Face* f = &emplace_back(faces);

                    // load the face indices
                    line.skip(2); // skip 'f '

                    VertexDescr* vd0 = nullptr;
                    while (strview vertdescr = line.next(' '))
                    {
                        // when encountering quads or large polygons, we need to triangulate the mesh
                        // by tracking the first vertex descr and forming a fan; this requires convex polys
                        if (f->Count == 3)
                        {
                            // v[0], v[2], v[3]
                            VertexDescr* vd2 = &f->VDS[2];
                            f = &emplace_back(faces);
                            f->VDS[f->Count++] = *vd0;
                            f->VDS[f->Count++] = *vd2;
                            // v[3] is parsed below:
                        }
                        VertexDescr& vd = f->VDS[f->Count++];
                        if (strview v = vertdescr.next('/')) vd.v = v.to_int() - 1;
                        if (strview t = vertdescr.next('/')) vd.t = t.to_int() - 1;
                        if (strview n = vertdescr)           vd.n = n.to_int() - 1;
                        if (!vd0) vd0 = &vd;
                    }
                }
                //else if (c == 's')
                //{
                //    line.skip(2); // skip "s "
                //    line >> group->SmoothingGroup;
                //}
                else if (c == 'u' && memcmp(line.str, "usemtl", 6) == 0)
                {
                    line.skip(7); // skip "usemtl "
                    strview matName = line.next(' ');
                    CurrentGroup()->Mat = FindMat(matName);
                }
                else if (c == 'm' && memcmp(line.str, "mtllib", 6) == 0)
                {
                    line.skip(7); // skip "mtllib "
                    strview matLib = line.next(' ');
                    materials = LoadMaterials(matLib);
                }
                else if (c == 'g')
                {
                    line.skip(2); // skip "g "
                    strview groupName = (string)line.next(' ');
                    group = &mesh.FindOrCreateGroup(groupName);
                }
                else if (c == 'o')
                {
                    line.skip(2); // skip "o "
                    mesh.Name = (string)line.next(' ');
                }
            }
        }

        void BuildMeshGroups()
        {
            size_t layerDataSize = (byte*)layersEnd - (byte*)vertsLayer;
            for (MeshGroup& g : mesh.Groups)
            {
                mesh.NumFaces += g.NumFaces();

                memset(vertsLayer, -1, layerDataSize);
                int usedVerts = 0, usedCoords = 0, usedNormals = 0, usedColors = 0;

                for (Face& face : g.Faces)
                {
                    for (VertexDescr& vd : face)
                    {
                        if (vd.v != -1) {
                            int& pos = vertsLayer[vd.v];
                            if (pos == -1) pos = usedVerts++;
                            vd.v = pos;
                        }
                        if (vd.t != -1) {
                            int& uvs = coordsLayer[vd.t];
                            if (uvs == -1) uvs = usedCoords++;
                            vd.v = uvs;
                        }
                        if (vd.n != -1) {
                            int& normal = normalsLayer[vd.n];
                            if (normal == -1) normal = usedNormals++;
                            vd.n = normal;
                        }
                        if (vd.c != -1) {
                            int& color = colorsLayer[vd.c];
                            if (color == -1) color = usedColors++;
                            vd.c = color;
                        }
                    }
                }

                auto mapLayerData = [&](auto dstVector, int usedCount, int totalCount, auto* srcData, int* layer)
                {
                    if (!usedCount) return;
                    dstVector.resize(usedCount);
                    auto* dst = dstVector.data();
                    for (int i = 0, j = 0; i < totalCount; ++i)
                    {
                        int layerIdx = layer[i];
                        if (layerIdx != -1)
                            dst[j++] = srcData[layerIdx];
                    }
                };

                mapLayerData(g.Verts,   usedVerts,   numVerts,   vertsData,   vertsLayer);
                mapLayerData(g.Coords,  usedCoords,  numCoords,  coordsData,  coordsLayer);
                mapLayerData(g.Normals, usedNormals, numNormals, normalsData, normalsLayer);
                mapLayerData(g.Colors,  usedColors,  numVerts,   colorsData,  colorsLayer);

                // assign per-vertex color ID-s, this is the only mode supported by OBJ
                if (usedColors) g.ColorMapping = MapPerVertex;
            }
        }
    };

    bool Mesh::LoadOBJ(const string& meshPath) noexcept
    {
        Clear();

        ObjLoader loader { *this, meshPath };

        if (!loader.parser) {
            println(stderr, "Failed to open file", meshPath);
            return false;
        }

        if (!loader.ProbeStats()) {
            fprintf(stderr, "Mesh::LoadOBJ() failed: No vertices in %s\n", meshPath.c_str());
            return false;
        }

        // OBJ maps vertex data globally, not per-mesh-group like most game engines expect
        // so this really complicates things when we build the mesh groups...
        // to speed up mesh loading, we use very heavy stack allocation

        // ObjLoader will free these in destructor if malloc was used
        // ReSharper disable once CppNonReclaimedResourceAcquisition
        void* mem = loader.bufferSize <= MaxStackAlloc
                    ? alloca(loader.bufferSize)
                    : malloc(loader.bufferSize);
        loader.InitPointers(mem);
        loader.ParseMeshData();
        loader.BuildMeshGroups();

        return true;
    }


    ///////////////////////////////////////////////////////////////////////////////////////////////

    static vector<Vector3> FlattenColors(const MeshGroup& group)
    {
        vector<Vector3> colors = { group.Verts.size(), Vector3::ZERO };

        for (const Face& face : group)
            for (const VertexDescr& vd : face)
            {
                if (vd.c == -1) continue;
                Vector3& dst = colors[vd.v];
                if (dst == Vector3::ZERO || dst == Vector3::ONE)
                    dst = group.Colors[vd.c];
            }
        return colors;
    }

    bool Mesh::SaveAsOBJ(const string& meshPath) const noexcept
    {
        if (file f = file{ meshPath, CREATENEW })
        {
            // straight to file, #dontcare about perf atm

            string matlib = file_replace_ext(meshPath, "mtl");
            if (SaveMaterials(*this, file_replace_ext(meshPath, "mtl")))
                f.writeln("mtllib", matlib);

            if (!Name.empty())
                f.writeln("o", Name);

            string buf;
            for (int group = 0; group < (int)Groups.size(); ++group)
            {
                const MeshGroup& g = Groups[group];
                if (!g.Name.empty()) f.writeln("g", g.Name);
                if (g.Mat)           f.writeln("usemtl", g.Mat->Name);
                f.writeln("s", group);

                auto* vertsData = g.Verts.data();
                if (g.Colors.empty())
                {
                    for (const Vector3& v : g.Verts)
                        f.writef("v %.6f %.6f %.6f\n", v.x, v.y, v.z);
                }
                else // non-standard extension for OBJ vertex colors
                {
                    // @todo Just leave a warning and export incorrect vertex colors?
                    assert((ColorMapping == MapPerVertex || ColorMapping == MapPerFaceVertex) 
                        && "OBJ export only supports per-vertex and per-face-vertex color mapping!");
                    assert(NumColors() >= NumVerts());

                    auto& colors = g.ColorMapping == MapPerFaceVertex ? FlattenColors(g) : g.Colors;
                    auto* colorsData = colors.data();

                    const int numVerts = g.NumVerts();
                    for (int i = 0; i < numVerts; ++i)
                    {
                        const Vector3& v = vertsData[i];
                        const Vector3& c = colorsData[i];
                        if (c == Vector3::ZERO) f.writef("v %.6f %.6f %.6f\n", v.x, v.y, v.z);
                        else f.writef("v %.6f %.6f %.6f %.6f %.6f %.6f\n", v.x, v.y, v.z, c.x, c.y, c.z);
                    }
                }

                for (const Vector2& v : g.Coords)  f.writef("vt %.4f %.4f\n", v.x, v.y);
                for (const Vector3& v : g.Normals) f.writef("vn %.4f %.4f %.4f\n", v.x, v.y, v.z);

                for (const Face& face : g.Faces)
                {
                    buf.clear();
                    buf += 'f';
                    for (const VertexDescr& vd : face)
                    {
                        buf += ' ', buf += to_string(vd.v + 1);
                        if (vd.t != -1) buf += '/', buf += to_string(vd.t + 1);
                        if (vd.n != -1) buf += '/', buf += to_string(vd.n + 1);
                    }
                    buf += '\n';
                    f.write(buf);
                }
            }
            for (const MeshGroup& g : Groups)
            {

            }
            return true;
        }
        fprintf(stderr, "Failed to create file '%s'\n", meshPath.c_str());
        return false;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
}
