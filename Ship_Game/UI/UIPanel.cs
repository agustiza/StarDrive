﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    /// <summary>
    /// A colored UI Panel that also behaves as a container for UI elements
    /// </summary>
    public class UIPanel : UIElementContainer, IColorElement
    {
        public SubTexture Texture;
        public SpriteAnimation SpriteAnim;
        public Color Color { get; set; }
        public bool DebugBorder;

        public override string ToString()
        {
            return Texture == null
                ? $"Panel {ElementDescr} Color={Color}"
                : $"Panel {ElementDescr} Name={Texture.Name}";
        }

        // Hint: use Color.TransparentBlack to create Panels with no fill
        public UIPanel(UIElementV2 parent, in Rectangle rect, Color color) : base(parent, rect)
        {
            Color = color;
        }

        public UIPanel(UIElementV2 parent, Vector2 pos, Vector2 size, Color color) : base(parent, pos, size)
        {
            Color = color;
        }

        public UIPanel(UIElementV2 parent, SubTexture tex, in Rectangle rect) : base(parent, rect)
        {
            Texture = tex;
            Color = Color.White;
        }
        
        public UIPanel(UIElementV2 parent, SubTexture tex, in Rectangle r, Color c) : base(parent, r)
        {
            Texture = tex;
            Color = c;
        }

        public UIPanel(UIElementV2 parent, string tex, int x, int y) : base(parent, new Vector2(x,y))
        {
            Texture = parent.ContentManager.LoadTextureOrDefault("Textures/"+tex);
            Size = Texture.SizeF;
            Color = Color.White;
        }

        public UIPanel(UIElementV2 parent, string tex, in Rectangle r) : base(parent, r)
        {
            Texture = parent.ContentManager.LoadTextureOrDefault("Textures/"+tex);
            Color = Color.White;
        }

        public UIPanel(UIElementV2 parent, string tex) : base(parent, Vector2.Zero)
        {
            Texture = parent.ContentManager.LoadTextureOrDefault("Textures/"+tex);
            Size = Texture.SizeF;
            Color = Color.White;
        }

        public UIPanel(in Rectangle rect, SubTexture texture, Color color) : base(null, rect)
        {
            Texture = texture;
            Color = color;
        }

        public UIPanel(in Rectangle rect, SpriteAnimation spriteAnim, Color color) : base(null, rect)
        {
            SpriteAnim = spriteAnim;
            Color = color;
        }

        public override void Update(float deltaTime)
        {
            SpriteAnim?.Update(deltaTime);
            base.Update(deltaTime);
        }

        public override void Draw(SpriteBatch batch)
        {
            if (SpriteAnim != null)
            {
                SpriteAnim.Draw(batch, Rect);
            }
            else if (Texture != null)
            {
                batch.Draw(Texture, Rect, Color);
            }
            else if (Color.A > 0)
            {
                batch.FillRectangle(Rect, Color);
            }

            if (DebugBorder)
            {
                batch.DrawRectangle(Rect, Color.Red);
            }
            base.Draw(batch);
        }
    }
}
