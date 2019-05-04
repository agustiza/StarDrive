using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game.Gameplay
{
    [Flags]
    public enum WeaponTag
    {
        Kinetic   = (1 << 0),
        Energy    = (1 << 1),
        Guided    = (1 << 2),
        Missile   = (1 << 3),
        Hybrid    = (1 << 4),
        Beam      = (1 << 5),
        Explosive = (1 << 6),
        Intercept = (1 << 7),
        Railgun   = (1 << 8),
        Bomb      = (1 << 9),
        SpaceBomb = (1 << 10),
        BioWeapon = (1 << 11),
        Drone     = (1 << 12),
        Warp      = (1 << 13),
        Torpedo   = (1 << 14),
        Cannon    = (1 << 15),
        Subspace  = (1 << 16),
        PD        = (1 << 17),
        Flak      = (1 << 18),
        Array     = (1 << 19),
        Tractor   = (1 << 20)
    }

    public sealed class Weapon : IDisposable, IDamageModifier
    {
        private WeaponTag TagBits;
        public bool this[WeaponTag tag]
        {
            get => (TagBits & tag) != 0;
            set => TagBits = value ? TagBits|tag : TagBits & ~tag;
        }
        private static readonly char[] TagBitsSeparator = {',', ' '};
        public string[] GetActiveTagIds()
        {
            string[] ids = TagBits.ToString().Split(TagBitsSeparator, StringSplitOptions.RemoveEmptyEntries);
            return ids;
        }
        public bool Tag_Kinetic   { get => this[WeaponTag.Kinetic];   set => this[WeaponTag.Kinetic]   = value; }
        public bool Tag_Energy    { get => this[WeaponTag.Energy];    set => this[WeaponTag.Energy]    = value; }
        public bool Tag_Guided    { get => this[WeaponTag.Guided];    set => this[WeaponTag.Guided]    = value; }
        public bool Tag_Missile   { get => this[WeaponTag.Missile];   set => this[WeaponTag.Missile]   = value; }
        public bool Tag_Hybrid    { get => this[WeaponTag.Hybrid];    set => this[WeaponTag.Hybrid]    = value; }
        public bool Tag_Beam      { get => this[WeaponTag.Beam];      set => this[WeaponTag.Beam]      = value; }
        public bool Tag_Explosive { get => this[WeaponTag.Explosive]; set => this[WeaponTag.Explosive] = value; }
        public bool Tag_Intercept { get => this[WeaponTag.Intercept]; set => this[WeaponTag.Intercept] = value; }
        public bool Tag_Railgun   { get => this[WeaponTag.Railgun];   set => this[WeaponTag.Railgun]   = value; }
        public bool Tag_Bomb      { get => this[WeaponTag.Bomb];      set => this[WeaponTag.Bomb]      = value; }
        public bool Tag_SpaceBomb { get => this[WeaponTag.SpaceBomb]; set => this[WeaponTag.SpaceBomb] = value; }
        public bool Tag_BioWeapon { get => this[WeaponTag.BioWeapon]; set => this[WeaponTag.BioWeapon] = value; }
        public bool Tag_Drone     { get => this[WeaponTag.Drone];     set => this[WeaponTag.Drone]     = value; }
        public bool Tag_Warp      { get => this[WeaponTag.Warp];      set => this[WeaponTag.Warp]      = value; }
        public bool Tag_Torpedo   { get => this[WeaponTag.Torpedo];   set => this[WeaponTag.Torpedo]   = value; }
        public bool Tag_Cannon    { get => this[WeaponTag.Cannon];    set => this[WeaponTag.Cannon]    = value; }
        public bool Tag_Subspace  { get => this[WeaponTag.Subspace];  set => this[WeaponTag.Subspace]  = value; }
        public bool Tag_PD        { get => this[WeaponTag.PD];        set => this[WeaponTag.PD]        = value; }
        public bool Tag_Flak      { get => this[WeaponTag.Flak];      set => this[WeaponTag.Flak]      = value; }
        public bool Tag_Array     { get => this[WeaponTag.Array];     set => this[WeaponTag.Array]     = value; }
        public bool Tag_Tractor   { get => this[WeaponTag.Tractor];   set => this[WeaponTag.Tractor]   = value; }

        [XmlIgnore][JsonIgnore] public Ship Owner { get; set; }
        [XmlIgnore][JsonIgnore] public GameplayObject drowner; // drone owner
        public float HitPoints;
        public bool isBeam;
        public float EffectVsArmor = 1f;
        public float EffectVSShields = 1f;
        public bool TruePD;
        public float TroopDamageChance;
        public float MassDamage;
        public float BombPopulationKillPerHit;
        public int BombTroopDamage_Min;
        public int BombTroopDamage_Max;
        public int BombHardDamageMin;
        public int BombHardDamageMax;
        public string HardCodedAction;
        public float RepulsionDamage;
        public float EMPDamage;
        public float ShieldPenChance;
        public float PowerDamage;
        public float SiphonDamage;
        public int BeamThickness;
        public float BeamDuration;
        public int BeamPowerCostPerSecond;
        public string BeamTexture;
        public int Animated;
        public int Frames;
        public string AnimationPath;
        public ExplosionType ExplosionType = ExplosionType.Projectile;
        public string dieCue;
        public string ToggleSoundName = "";
        [XmlIgnore][JsonIgnore] readonly AudioHandle ToggleCue = new AudioHandle();
        public string Light;
        public bool isTurret;
        public bool isMainGun;
        public float OrdinanceRequiredToFire;
        public Vector2 Center;
        public float Range;
        public float DamageAmount;
        public float ProjectileSpeed;
        public int ProjectileCount = 1;
        public int FireArc; // misleading; this is the FireDispersionArc for cannons that do salvo fire
        public int FireCone; // this is the fire imprecision cone, direction spread is randomized [-FireCone,+FireCone]
        public string ProjectileTexturePath;
        public string ModelPath;
        public string WeaponType;
        public string WeaponEffectType;
        public string UID;
        [XmlIgnore][JsonIgnore] public ShipModule Module;
        [XmlIgnore][JsonIgnore] public float CooldownTimer;
        public float fireDelay;
        public float PowerRequiredToFire;
        public bool explodes;
        public float DamageRadius;
        public string fireCueName;
        public string MuzzleFlash;
        public bool IsRepairDrone;
        public bool FakeExplode;
        public float ProjectileRadius = 4f;
        public string Name;
        public byte LoopAnimation;
        public float Scale = 1f;
        public float RotationRadsPerSecond = 2f;
        public string InFlightCue = "";
        public float particleDelay;
        public float ECMResist;
        public bool Excludes_Fighters;
        public bool Excludes_Corvettes;
        public bool Excludes_Capitals;
        public bool Excludes_Stations;
        public bool isRepairBeam;
        public bool TerminalPhaseAttack;
        public float TerminalPhaseDistance;
        public float TerminalPhaseSpeedMod = 2f;
        public float ArmourPen = 0f;
        public string SecondaryFire;
        public bool AltFireMode;
        public bool AltFireTriggerFighter;
        private Weapon AltFireWeapon;
        public float OffPowerMod = 1f;
        public bool RangeVariance;
        public float ExplosionRadiusVisual = 4.5f;
        [XmlIgnore][JsonIgnore] public GameplayObject FireTarget { get; private set; }
        private float TargetChangeTimer;
        public bool UseVisibleMesh;
        public bool PlaySoundOncePerSalvo; // @todo DEPRECATED
        public int SalvoSoundInterval = 1; // play sound effect every N salvos
        public int SalvoCount = 1;
        public float SalvoTimer;

        [XmlIgnore][JsonIgnore] public int SalvosToFire { get; private set; }
        private float SalvoDirection;
        private float SalvoFireTimer; // while SalvosToFire, use this timer to count when to fire next shot
        private GameplayObject SalvoTarget;
        public float ECM = 0;

        public void InitializeTemplate()
        {
            BeamDuration = BeamDuration > 0 ? BeamDuration : 2f;
            fireDelay = Math.Max(0.016f, fireDelay);
            SalvoTimer = Math.Max(0, SalvoTimer);
            if (PlaySoundOncePerSalvo) // @note Backwards compatibility
                SalvoSoundInterval = 9999;
            if (Tag_Missile)
            {
                if (WeaponType.IsEmpty()) WeaponType = "Missile";
                else if (WeaponType != "Missile")
                {
                    Log.Warning($"Weapon '{UID}' has 'tag_missile' but Weapontype is '{WeaponType}' instead of missile. This Causes invisible projectiles.");
                }
            }
        }

        public Weapon(Ship owner, ShipModule module)
        {
            Owner  = owner;
            Module = module;
        }

        public Weapon()
        {
            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo != null)
                ExplosionRadiusVisual *= GlobalStats.ActiveModInfo.GlobalExplosionVisualIncreaser;
        }

        public Weapon Clone()
        {
            var wep = (Weapon)MemberwiseClone();
            wep.SalvoTarget = null;
            wep.FireTarget  = null;
            wep.Module      = null;
            wep.Owner       = null;
            wep.drowner     = null;
            return wep;
        }

        [XmlIgnore][JsonIgnore]
        public float NetFireDelay =>  isBeam ? fireDelay + BeamDuration : fireDelay + SalvoTimer;

        [XmlIgnore][JsonIgnore]
        public float OrdnanceUsagePerSecond => OrdinanceRequiredToFire * ProjectileCount * SalvoCount / NetFireDelay;

        [XmlIgnore][JsonIgnore] // only usage during fire, not power maintenance
        public float PowerFireUsagePerSecond => (BeamPowerCostPerSecond * BeamDuration + PowerRequiredToFire * ProjectileCount * SalvoCount) / NetFireDelay;

        // modify damageamount utilizing tech bonus. Currently this is only ordnance bonus.
        public float GetDamageWithBonuses(Ship owner)
        {
            float damageAmount = DamageAmount;
            if (owner?.loyalty?.data != null && OrdinanceRequiredToFire > 0)
                damageAmount += damageAmount * owner.loyalty.data.OrdnanceEffectivenessBonus;

            if (owner?.Level > 0)
                damageAmount += damageAmount * owner.Level * 0.05f;

            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.useHullBonuses) // Hull bonus damage increase
                if (ResourceManager.HullBonuses.TryGetValue(owner.shipData.Hull, out HullBonus mod))
                    damageAmount += damageAmount * mod.DamageBonus;

            return damageAmount;
        }


        public void PlayToggleAndFireSfx(AudioEmitter emitter = null)
        {
            if (ToggleCue.IsPlaying)
                return;

            AudioEmitter soundEmitter = emitter ?? Owner?.SoundEmitter;
            GameAudio.PlaySfxAsync(fireCueName, soundEmitter);
            ToggleCue.PlaySfxAsync(ToggleSoundName, soundEmitter);
        }

        public void FireDrone(Vector2 direction)
        {
            if (CanFireWeaponCooldown())
            {
                PrepareToFire();
                Projectile.Create(this, Module.Center, direction, null, playSound: true);
            }
        }

        public Vector2 GetFireConeSpread(Vector2 direction)
        {
            if (FireCone <= 0)
                return direction;
            float spread = RandomMath2.RandomBetween(-FireCone, FireCone) * 0.5f;
            return (direction.ToDegrees() + spread).AngleToDirection();
        }

        struct FireSource
        {
            public readonly Vector2 Origin;
            public readonly Vector2 Direction;
            public FireSource(Vector2 origin, Vector2 direction)
            {
                Origin    = origin;
                Direction = direction;
            }
        }

        IEnumerable<FireSource> EnumFireSources(Vector2 origin, Vector2 direction)
        {
            if (FireArc != 0)
            {
                float degreesBetweenShots = FireArc / (float)ProjectileCount;
                float angleToTarget = direction.ToDegrees() - FireArc * 0.5f;
                for (int i = 0; i < ProjectileCount; ++i)
                {
                    Vector2 dir = angleToTarget.AngleToDirection();
                    angleToTarget += degreesBetweenShots;
                    yield return new FireSource(origin, GetFireConeSpread(dir));
                }
            }
            else
            {
                for (int i = 0; i < ProjectileCount; ++i)
                {
                    yield return new FireSource(origin, GetFireConeSpread(direction));
                }
            }
        }

        void SpawnSalvo(Vector2 direction, GameplayObject target, bool playSound)
        {
            bool secondary = SecondaryFire != null && AltFireTriggerFighter && AltFireMode
                && target is ShipModule shipModule && shipModule.GetParent().shipData.Role == ShipData.RoleName.fighter;

            if (secondary && AltFireWeapon == null)
                AltFireWeapon = ResourceManager.CreateWeapon(SecondaryFire);

            Weapon  weapon = secondary ? AltFireWeapon : this;
            Vector2 origin = Origin;

            foreach (FireSource fireSource in EnumFireSources(origin, direction))
            {
                Projectile.Create(weapon, fireSource.Origin, fireSource.Direction, target, playSound);
                playSound = false; // only play sound once per fire cone
            }
        }

        bool CanFireWeapon()
        {
            return Module.Active && Module.Powered
                && Owner.engineState  != Ship.MoveState.Warp
                && Owner.PowerCurrent >= PowerRequiredToFire
                && Owner.Ordinance    >= OrdinanceRequiredToFire;
        }

        bool CanFireWeaponCooldown()
        {
            return CooldownTimer <= 0f && CanFireWeapon();
        }

        void PrepareToFire()
        {
            // cooldown should start after all salvos have finished, so
            // increase the cooldown by SalvoTimer
            CooldownTimer = NetFireDelay + RandomMath.RandomBetween(-10f, +10f) * 0.008f;

            Owner.InCombatTimer = 15f;
            Owner.ChangeOrdnance(-OrdinanceRequiredToFire);
            Owner.PowerCurrent -= PowerRequiredToFire;
        }

        bool PrepareToFireSalvo()
        {
            float timeBetweenShots = SalvoTimer / SalvoCount;
            if (SalvoFireTimer < timeBetweenShots || !CanFireWeapon())
                return false; // not ready to fire salvo

            SalvoFireTimer -= timeBetweenShots;
            --SalvosToFire;

            Owner.InCombatTimer = 15f;
            Owner.ChangeOrdnance(-OrdinanceRequiredToFire);
            Owner.PowerCurrent -= PowerRequiredToFire;
            return true;
        }

        void ContinueSalvo()
        {
            if (SalvoTarget != null && !Owner.CheckRangeToTarget(this, SalvoTarget))
                return;

            int salvoIndex = SalvoCount - SalvosToFire;
            if (!PrepareToFireSalvo())
                return;

            if (SalvoTarget != null) // check for new direction
            {
                // update direction only if we have a new valid pip
                if (PrepareFirePos(SalvoTarget, SalvoTarget.Center, out Vector2 firePos))
                    SalvoDirection = (firePos - Module.Center).ToRadians() - Owner.Rotation;
            }

            bool playSound = salvoIndex % SalvoSoundInterval == 0;
            SpawnSalvo((SalvoDirection + Owner.Rotation).RadiansToDirection(), SalvoTarget, playSound);
        }

        bool PrepareFirePos(GameplayObject target, Vector2 targetPos, out Vector2 firePos)
        {
            if (target != null)
            {
                if (ProjectedImpactPoint(target, out Vector2 pip) && CheckFireArc(pip))
                {
                    firePos = pip;
                    return true;
                }
            }

            // if no target OR pip was not in arc, check if targetPos itself is in arc
            if (CheckFireArc(targetPos))
            {
                firePos = targetPos;
                return true;
            }
            firePos = targetPos;
            return false;
        }

        bool FireAtTarget(Vector2 targetPos, GameplayObject target = null)
        {
            if (!PrepareFirePos(target, targetPos, out Vector2 firePos))
                return false;

            PrepareToFire();
            Vector2 direction = Origin.DirectionToTarget(firePos);
            SpawnSalvo(direction, target, playSound:true);

            if (SalvoCount > 1)  // queue the rest of the salvo to follow later
            {
                SalvosToFire   = SalvoCount - 1;
                SalvoDirection = direction.ToRadians() - Owner.Rotation; // keep direction relative to source
                SalvoFireTimer = 0f;
                SalvoTarget    = target;
            }
            return true;
        }

        public void ClearFireTarget()
        {
            FireTarget  = null;
            SalvoTarget = null;
        }

        // > 66% accurate or >= Level 5 crews can use advanced
        // targeting which even predicts acceleration
        public bool CanUseAdvancedTargeting =>
            (Module != null && Module.AccuracyPercent > 0.66f) ||
            (Owner  != null && Owner.CanUseAdvancedTargeting);

        Vector2 GetLevelBasedError(int level = -1)
        {
            if (Module == null || Module.AccuracyPercent > 0.9999f || TruePD)
                return Vector2.Zero; //|| Tag_PD

            if (level == -1)
            {
                level = (Owner?.Level ?? 0)
                      + (Owner?.TrackingPower ?? 0)
                      + (Owner?.loyalty?.data.Traits.Militaristic ?? 0);
            }

            // reduce error by level cubed. if error is less than a module radius stop.
            float baseError = 45f + 8 * Module.XSIZE * Module.YSIZE;
            float adjust = Math.Max(0, baseError - level * level * level);
            if (adjust < 8)
                return Vector2.Zero;

            // reduce or increase error based on weapon and trait characteristics.
            if (Tag_Cannon)  adjust *= (1f - (Owner?.loyalty?.data.Traits.EnergyDamageMod ?? 0));
            if (Tag_Kinetic) adjust *= (1f - (Owner?.loyalty?.data.OrdnanceEffectivenessBonus ?? 0));

            adjust *= CalculateBaseAccuracy();

            return RandomMath2.Vector2D(adjust);
        }

        float CalculateBaseAccuracy()
        {
            float accuracy = (Module?.AccuracyPercent ?? 0f);
            if (accuracy.AlmostEqual(-1f))
            {
                accuracy = 1f;
                if (isTurret) accuracy *= 0.5f;
                if (Tag_PD)   accuracy *= 0.5f;
                if (TruePD)   accuracy *= 0.5f;
            }
            else
            {
                accuracy = 1f - accuracy;
            }
            return accuracy;
        }

        // @note This is used for debugging
        [XmlIgnore][JsonIgnore]
        public Vector2 DebugLastImpactPredict { get; private set; }

        public Vector2 GetTargetError(GameplayObject target, int level = -1)
        {
            Vector2 error = GetLevelBasedError(level); // base error from crew level/targeting bonuses
            if (target != null)
            {
                error += target.JammingError(); // if target has ECM, they can scramble their position
            }
            return error;
        }

        // Applies correction to weapon target based on distance to target
        public Vector2 AdjustedImpactPoint(Vector2 source, Vector2 target, Vector2 error)
        {
            // once we get too close the angle error becomes too big, so move the target pos further
            float distance = source.Distance(target);
            const float minDistance = 500f;
            if (distance < minDistance)
            {
                // move pip forward a bit
                target += (minDistance - distance)*source.DirectionToTarget(target);
            }
            
            // total error magnitude should get smaller as we get closer
            float errorMagnitude = 1f;
            if (distance < 2000f)
            {
                errorMagnitude = (distance+minDistance) / 2000f;
            }

            Vector2 adjusted = target + error*errorMagnitude;
            DebugLastImpactPredict = adjusted;
            return adjusted;
        }

        
        public Vector2 Origin => Module?.Center ?? Center;
        public Vector2 OwnerVelocity => Owner?.Velocity ?? Module?.GetParent()?.Velocity ?? Vector2.Zero;

        Vector2 Predict(Vector2 origin, GameplayObject target, bool advancedTargeting)
        {
            Vector2 pip = new ImpactPredictor(origin, OwnerVelocity, ProjectileSpeed, target)
                .Predict(advancedTargeting);

            float distance = origin.Distance(pip);
            float maxPredictionRange = Range*2;
            if (distance > maxPredictionRange)
            {
                Vector2 predictionVector = target.Center.DirectionToTarget(pip);
                pip = target.Center + predictionVector*maxPredictionRange;
            }

            return pip;
        }

        public bool ProjectedImpactPointNoError(GameplayObject target, out Vector2 pip)
        {
            pip = Predict(Origin, target, advancedTargeting: true);
            return pip != Vector2.Zero;
        }

        public bool ProjectedImpactPoint(GameplayObject target, out Vector2 pip)
        {
            Vector2 origin = Origin;
            pip = Predict(origin, target, CanUseAdvancedTargeting);
            if (pip == Vector2.Zero)
                return false;
            pip = AdjustedImpactPoint(origin, pip, GetTargetError(target));
            return true;
        }
        
        // Prioritizes mainTarget
        // But if mainTarget is not viable, will pick a new Target using ships/projectiles lists
        public void UpdateAndFireAtTarget(Ship mainTarget,
            Array<Projectile> enemyProjectiles, Array<Ship> enemyShips)
        {
            TargetChangeTimer -= 0.0167f;
            if (!CanFireWeaponCooldown())
                return; // we can't fire, so don't bother checking targets

            if (ShouldPickNewTarget())
            {
                ClearFireTarget();
                if (!PickProjectileTarget(enemyProjectiles))
                {
                    PickShipTarget(mainTarget, enemyShips);
                }
            }
            
            // FB: reason for ghost target is that FireTarget is not null even if the mainTarget is dead. 
            // so i added a check for main target, but this might be only a workaround
            if (FireTarget != null && mainTarget?.Active == true)
            {
                if (isBeam)
                    FireBeam(Module.Center, FireTarget.Center, FireTarget);
                else
                    FireAtTarget(FireTarget.Center, FireTarget);
            }
        }

        bool IsTargetAliveAndInRange(GameplayObject target)
        {
            if (target == null || !target.Active || (target is Ship ship && ship.dying))
                return false;
            return Owner.IsTargetInFireArcRange(this, target);
        }

        bool ShouldPickNewTarget()
        {
            // Reasons for this weapon not to choose a new target
            if (TargetChangeTimer > 0f // not ready yet
                || IsRepairDrone // TODO: is this correct?
                || isRepairBeam  // TODO: repair beams are managed by repair drone ai?
                || SalvosToFire > 0) // we are still firing salvos
                return false;

            if (!IsTargetAliveAndInRange(FireTarget))
            {
                FireTarget = null;
                TargetChangeTimer = 0.1f * Math.Max(Module.XSIZE, Module.YSIZE);
                // cumulative bonuses for turrets, PD and true PD weapons
                if (isTurret) TargetChangeTimer *= 0.5f;
                if (Tag_PD)   TargetChangeTimer *= 0.5f;
                if (TruePD)   TargetChangeTimer *= 0.25f;
                return true;
            }
            return false; // Target is ok
        }

        public bool TargetValid(GameplayObject fireTarget)
        {
            if (fireTarget.Type == GameObjectType.ShipModule)
                return TargetValid(((ShipModule)fireTarget).GetParent().shipData.HullRole);
            if (fireTarget.Type == GameObjectType.Ship)
                return TargetValid(((Ship)fireTarget).shipData.HullRole);
            return true;
        }

        bool PickProjectileTarget(Array<Projectile> enemyProjectiles)
        {
            if (Tag_PD && enemyProjectiles.NotEmpty)
            {
                int maxTracking = Owner.TrackingPower + Owner.Level;
                for (int i = 0; i < maxTracking && i < enemyProjectiles.Count; i++)
                {
                    Projectile proj = enemyProjectiles[i];
                    if (proj.Active && proj.Health > 0f
                        && proj.Weapon.Tag_Intercept // projectile can be intercepted?
                        && Owner.IsTargetInFireArcRange(this, proj))
                    {
                        FireTarget = proj;
                        return true;
                    }
                }
            }
            return false;
        }

        void PickShipTarget(Ship mainTarget, Array<Ship> potentialTargets)
        {
            if (TruePD) // true PD weapons can't target ships
                return;

            Ship newTargetShip = null;

            // prefer our main target:
            if (IsTargetAliveAndInRange(mainTarget) && TargetValid(mainTarget))
            {
                newTargetShip = mainTarget;
            }
            else // otherwise track a new target:
            {
                // limit to one target per level.
                int maxTracking = Owner.TrackingPower + Owner.Level;
                for (int i = 0; i < potentialTargets.Count && i < maxTracking; ++i)
                {
                    Ship potentialTarget = potentialTargets[i];
                    if (TargetValid(potentialTarget) && IsTargetAliveAndInRange(potentialTarget))
                    {
                        newTargetShip = potentialTarget;
                        break;
                    }
                }
            }

            // now choose a random module to target
            if (newTargetShip != null)
            {
                FireTarget = newTargetShip.GetRandomInternalModule(this);
            }
        }

        bool CheckFireArc(Vector2 targetPos, GameplayObject maybeTarget = null)
        {
            return maybeTarget != null
                ? Owner.IsTargetInFireArcRange(this, maybeTarget)
                : Owner.IsInsideFiringArc(this, targetPos);
        }

        public Vector2 ProjectedBeamPoint(Vector2 source, Vector2 destination, GameplayObject target = null)
        {
            if (DamageAmount < 1)
                return destination;

            if (target != null && !Owner.loyalty.IsEmpireAttackable(target.GetLoyalty()))
                return destination;

            if (Tag_Tractor || isRepairBeam)
                return destination;

            Vector2 beamDestination = AdjustedImpactPoint(source, destination, GetTargetError(target));
            return beamDestination;
        }

        bool FireBeam(Vector2 source, Vector2 destination, GameplayObject target = null)
        {
            destination = ProjectedBeamPoint(source, destination, target);
            if (!Owner.IsInsideFiringArc(this, destination))
                return false;

            PrepareToFire();
            var beam = new Beam(this, source, destination, target);
            Module.GetParent().AddBeam(beam);
            return true;
        }

        public DroneBeam FireDroneBeam(DroneAI droneAI)
        {
            drowner = droneAI.Drone;
            return new DroneBeam(droneAI);
        }

        public void FireTargetedBeam(GameplayObject target)
        {
            FireBeam(Module.Center, target.Center, target);
        }

        public bool ManualFireTowardsPos(Vector2 targetPos)
        {
            if (CanFireWeapon())
            {
                if (isBeam)
                {
                    return FireBeam(Module.Center, targetPos, null);
                }
                return FireAtTarget(targetPos, null);
            }
            return false;
        }

        public void FireFromPlanet(Planet planet, Ship targetShip)
        {
            targetShip.InCombatTimer = 15f;
            GameplayObject target = targetShip.GetRandomInternalModule(this) ?? (GameplayObject) targetShip;

            if (isBeam)
            {
                FireBeam(planet.Center, target.Center, target);
                return;
            }

            if (ProjectedImpactPoint(target, out Vector2 pip))
            {
                Vector2 direction = (pip - Center).Normalized();

                foreach (FireSource fireSource in EnumFireSources(planet.Center, direction))
                    Projectile.Create(this, planet, fireSource.Direction, target);
            }
        }

        // @todo This requires optimization
        public void ModifyProjectile(Projectile projectile)
        {
            if (Owner == null)
                return;
            if (Owner.loyalty.data.Traits.Pack)
                projectile.DamageAmount += projectile.DamageAmount * Owner.PackDamageModifier;

            //if (GlobalStats.HasMod && !GlobalStats.ActiveModInfo.useWeaponModifiers)
            //    return;

            float actualshieldpenchance = 0;

            if (Tag_Missile)   AddModifiers("Missile", projectile, ref actualshieldpenchance);
            if (Tag_Energy)    AddModifiers("Energy",  projectile, ref actualshieldpenchance);
            if (Tag_Torpedo)   AddModifiers("Torpedo", projectile, ref actualshieldpenchance);
            if (Tag_Kinetic)   AddModifiers("Kinetic", projectile, ref actualshieldpenchance);
            if (Tag_Hybrid)    AddModifiers("Hybrid",  projectile, ref actualshieldpenchance);
            if (Tag_Railgun)   AddModifiers("Railgun", projectile, ref actualshieldpenchance);
            if (Tag_Explosive) AddModifiers("Explosive", projectile, ref actualshieldpenchance);
            if (Tag_Guided)    AddModifiers("Guided",    projectile, ref actualshieldpenchance);
            if (Tag_Intercept) AddModifiers("Intercept", projectile, ref actualshieldpenchance);
            if (Tag_PD)        AddModifiers("PD",        projectile, ref actualshieldpenchance);
            if (Tag_SpaceBomb) AddModifiers("Spacebomb", projectile, ref actualshieldpenchance);
            if (Tag_BioWeapon) AddModifiers("BioWeapon", projectile, ref actualshieldpenchance);
            if (Tag_Drone)     AddModifiers("Drone",     projectile, ref actualshieldpenchance);
            if (Tag_Subspace)  AddModifiers("Subspace",  projectile, ref actualshieldpenchance);
            if (Tag_Warp)      AddModifiers("Warp",    projectile, ref actualshieldpenchance);
            if (Tag_Cannon)    AddModifiers("Cannon",  projectile, ref actualshieldpenchance);
            if (Tag_Beam)      AddModifiers("Beam",    projectile, ref actualshieldpenchance);
            if (Tag_Bomb)      AddModifiers("Bomb",    projectile, ref actualshieldpenchance);
            if (Tag_Array)     AddModifiers("Array",   projectile, ref actualshieldpenchance);
            if (Tag_Flak)      AddModifiers("Flak",    projectile, ref actualshieldpenchance);
            if (Tag_Tractor)   AddModifiers("Tractor", projectile, ref actualshieldpenchance);

            projectile.IgnoresShields = actualshieldpenchance > 0f && RandomMath2.InRange(100) <= actualshieldpenchance;
        }

        void AddModifiers(string tag, Projectile projectile, ref float actualShieldPenChance)
        {
            SerializableDictionary<string, WeaponTagModifier> wepTags = Owner.loyalty.data.WeaponTags;
            projectile.DamageAmount      += wepTags[tag].Damage * projectile.DamageAmount;
            projectile.ShieldDamageBonus += wepTags[tag].ShieldDamage;
            projectile.ArmorDamageBonus  += wepTags[tag].ArmorDamage;
            // Shield Penetration
            float currenshieldpenchance   = Module.GetParent().loyalty.data.ShieldPenBonusChance; // check the old calcs first
            currenshieldpenchance        += wepTags[tag].ShieldPenetration * 100; // new calcs
            currenshieldpenchance        += ShieldPenChance;
            actualShieldPenChance = Math.Max(currenshieldpenchance, actualShieldPenChance);
            if (isBeam) return;
            projectile.ArmorPiercing         += (int)wepTags[tag].ArmourPenetration;
            projectile.Health                += HitPoints * wepTags[tag].HitPoints;
            projectile.RotationRadsPerSecond += wepTags[tag].Turn * RotationRadsPerSecond;
            projectile.Speed                 += wepTags[tag].Speed * ProjectileSpeed;
            projectile.DamageRadius          += wepTags[tag].ExplosionRadius * DamageRadius;
        }

        public void ResetToggleSound()
        {
            if (ToggleCue.IsPlaying)
                ToggleCue.Stop();
        }

        public void Update(float elapsedTime)
        {
            if (CooldownTimer > 0f)
            {
                if (WeaponType != "Drone")
                    CooldownTimer = MathHelper.Max(CooldownTimer - elapsedTime, 0f);
            }

            if (SalvosToFire > 0)
            {
                SalvoFireTimer += elapsedTime;
                ContinueSalvo();
            }
            else SalvoTarget = null;
            Center = Module.Center;
        }

        float CachedModifiedRange;
        public float GetModifiedRange()
        {
            if (Owner?.loyalty == null || GlobalStats.ActiveModInfo == null || !GlobalStats.ActiveModInfo.useWeaponModifiers)
                return Range;

            if (CachedModifiedRange > 0f)
                return CachedModifiedRange;

            float modifier = 1.0f;
            EmpireData loyaltyData = Owner.loyalty.data;
            if (Tag_Beam)      modifier *= loyaltyData.WeaponTags["Beam"].Range;
            if (Tag_Energy)    modifier *= loyaltyData.WeaponTags["Energy"].Range;
            if (Tag_Explosive) modifier *= loyaltyData.WeaponTags["Explosive"].Range;
            if (Tag_Guided)    modifier *= loyaltyData.WeaponTags["Guided"].Range;
            if (Tag_Hybrid)    modifier *= loyaltyData.WeaponTags["Hybrid"].Range;
            if (Tag_Intercept) modifier *= loyaltyData.WeaponTags["Intercept"].Range;
            if (Tag_Kinetic)   modifier *= loyaltyData.WeaponTags["Kinetic"].Range;
            if (Tag_Missile)   modifier *= loyaltyData.WeaponTags["Missile"].Range;
            if (Tag_Railgun)   modifier *= loyaltyData.WeaponTags["Railgun"].Range;
            if (Tag_Cannon)    modifier *= loyaltyData.WeaponTags["Cannon"].Range;
            if (Tag_PD)        modifier *= loyaltyData.WeaponTags["PD"].Range;
            if (Tag_SpaceBomb) modifier *= loyaltyData.WeaponTags["Spacebomb"].Range;
            if (Tag_BioWeapon) modifier *= loyaltyData.WeaponTags["BioWeapon"].Range;
            if (Tag_Drone)     modifier *= loyaltyData.WeaponTags["Drone"].Range;
            if (Tag_Subspace)  modifier *= loyaltyData.WeaponTags["Subspace"].Range;
            if (Tag_Warp)      modifier *= loyaltyData.WeaponTags["Warp"].Range;
            if (Tag_Array)     modifier *= loyaltyData.WeaponTags["Array"].Range;
            if (Tag_Flak)      modifier *= loyaltyData.WeaponTags["Flak"].Range;
            if (Tag_Tractor)   modifier *= loyaltyData.WeaponTags["Tractor"].Range;

            CachedModifiedRange = modifier * Range;
            return CachedModifiedRange;
        }


        public float GetShieldDamageMod(ShipModule module)
        {
            float damageModifier = EffectVSShields;
            if      (Tag_Kinetic) damageModifier *= (1f - module.shield_kinetic_resist);
            else if (Tag_Energy)  damageModifier *= (1f - module.shield_energy_resist);
            else if (Tag_Beam)    damageModifier *= (1f - module.shield_beam_resist);
            else if (Tag_Missile) damageModifier *= (1f - module.shield_missile_resist);
            return damageModifier;
        }

        public float GetArmorDamageMod(ShipModule module)
        {
            float damageModifier = 1f;
            if (module.Is(ShipModuleType.Armor)) damageModifier *= EffectVsArmor;
            if (Tag_Explosive)                   damageModifier *= (1f - module.ExplosiveResist);
            if (Tag_Kinetic)                     damageModifier *= (1f - module.KineticResist);
            else if (Tag_Beam)                   damageModifier *= (1f - module.BeamResist);
            else if (Tag_Energy)                 damageModifier *= (1f - module.EnergyResist);
            else if (Tag_Missile)                damageModifier *= (1f - module.MissileResist);
            else if (Tag_Torpedo)                damageModifier *= (1f - module.TorpedoResist);
            return damageModifier;
        }

        public bool TargetValid(ShipData.RoleName role)
        {
            if (Excludes_Fighters && (role == ShipData.RoleName.fighter || role == ShipData.RoleName.scout || role == ShipData.RoleName.drone))
                return false;
            if (Excludes_Corvettes && (role == ShipData.RoleName.corvette || role == ShipData.RoleName.gunboat))
                return false;
            if (Excludes_Capitals && (role == ShipData.RoleName.frigate || role == ShipData.RoleName.destroyer || role == ShipData.RoleName.cruiser || role == ShipData.RoleName.carrier || role == ShipData.RoleName.capital))
                return false;
            if (Excludes_Stations && (role == ShipData.RoleName.platform || role == ShipData.RoleName.station))
                return false;
            return true;
        }

        public float UpdateOffense(ShipModule m = null)
        {
            float off = 0f;
            if (isBeam)
            {
                off += DamageAmount * 90 * BeamDuration * (1f / NetFireDelay);
                off += MassDamage * 30 * (1f / NetFireDelay);
                off += PowerDamage * 45 * (1f / NetFireDelay);
                off += RepulsionDamage * 45 * (1f / NetFireDelay);
                off += SiphonDamage * 45 * (1f / NetFireDelay);
                off += TroopDamageChance * (1f / NetFireDelay);
            }
            else
            {
                off += DamageAmount * SalvoCount * ProjectileCount * (1f / NetFireDelay);
                off += EMPDamage * SalvoCount * ProjectileCount * (1f / NetFireDelay) * .5f;
            }

            //Doctor: Guided weapons attract better offensive rating than unguided - more likely to hit. Setting at flat 25% currently.
            off *= Tag_Guided ? 1.25f : 1f;

            //FB: Range margins are less steep for missiles
            off *= !Tag_Missile && !Tag_Torpedo ? (Range / 4000) * (Range / 4000) : (Range / 4000);

            // FB: simpler calcs for these.
            off *= EffectVsArmor > 1 ? 1f + (EffectVsArmor - 1f) / 2f : 1f;
            off *= EffectVsArmor < 1 ? 1f - (1f - EffectVsArmor) / 2f : 1f;
            off *= EffectVSShields > 1 ? 1f + (EffectVSShields - 1f) / 2f : 1f;
            off *= EffectVSShields < 1 ? 1f - (1f - EffectVSShields) / 2f : 1f;

            off *= TruePD ? .2f : 1f;
            off *= Tag_Intercept && Tag_Missile ? .8f : 1f;
            off *= ProjectileSpeed > 1 ? ProjectileSpeed / 4000 : 1f;

            // FB: offense calcs for damage radius
            off *= DamageRadius > 24 && !TruePD ? DamageRadius / 24f : 1f;

            // FB: Added shield pen chance
            off *= 1 + ShieldPenChance / 100;

            int allRoles = 0;
            int restrictedRoles = 0;
            foreach (ShipData.RoleName role in Enum.GetValues(typeof(ShipData.RoleName)))
            {
                allRoles++;
                if (!TargetValid(role))
                    restrictedRoles++;
            }
            float restrictions = (float)(allRoles - restrictedRoles) / allRoles;
            off *= restrictions;

            //Doctor: If there are manual XML override modifiers to a weapon for manual balancing, apply them.
            if (m == null)
                return off * OffPowerMod;

            //FB: Kinetics which does also require more than minimal power to shoot is less effective
            off *= Tag_Kinetic && PowerRequiredToFire > 10 * m.Area ? 0.5f : 1f;

            //FB: Kinetics which does also require more than minimal power to maintain is less effective
            off *= Tag_Kinetic && m.PowerDraw > 2 * m.Area ? 0.5f : 1f;
            // FB: Turrets get some off
            off *= m.ModuleType == ShipModuleType.Turret ? 1.25f : 1f;

            // FB: Field of Fire is also important
            off *= m.FieldOfFire > 60 ? m.FieldOfFire / 60f : 1f;

            //Doctor: If there are manual XML override modifiers to a weapon for manual balancing, apply them.
            return off * OffPowerMod;
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        ~Weapon() { Destroy(); }

        void Destroy()
        {
            ToggleCue.Destroy();
            Owner         = null;
            drowner       = null;
            Module        = null;
            AltFireWeapon = null;
            FireTarget    = null;
            SalvoTarget   = null;
        }

        public override string ToString() => $"Weapon {WeaponType} {WeaponEffectType} {Name}";
    }
}
