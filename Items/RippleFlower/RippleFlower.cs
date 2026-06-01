using lsfUtils.Creatures;
using lsfUtils.CWTs;
using MoreSlugcats;
using Noise;
using RWCustom;
using Smoke;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using static lsfUtils.Plugin;
using Menu.Remix.MixedUI;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.Remoting.Contexts;
using System.Security.Permissions;
using Watcher;


namespace lsfUtils.Items.RippleFlower
{
    public class RippleFlower : KarmaFlower
    {
        public const int rippleSpaceDuration = 800;

        public RippleFlower(RippleFlowerAbstract abstr) : base(abstr)
        {
            Log.LogMessage("Spawning ripple flower!!");
            if (CWTs.KarmaFlowerCWT.TryGetData(this, out var data))
            {
                data.rippleFlower = true;
            }
            else
            {
                Log.LogMessage("Couldnt get karma flower CWT from ctor!");
            }
        }

        public static bool IsRippleFlower(KarmaFlower flower)
        {
            if (flower == null)
            {
                Log.LogMessage("Flower is null!");
                return false;
            }
            if (flower.abstractPhysicalObject?.Room?.world?.game?.StoryCharacter?.ToString() == "looker" || flower.room?.game?.StoryCharacter?.ToString() == "looker")
            {
                return true;
            }
            if (!CWTs.KarmaFlowerCWT.TryGetData(flower, out var data))
            {
                Log.LogMessage("Cannot get flower CWT!");
                return false;
            }
            return data.rippleFlower;
        }

        public static void KarmaFlower_BitByPlayer(On.KarmaFlower.orig_BitByPlayer orig, KarmaFlower self, Creature.Grasp grasp, bool eu)
        {
            if (IsRippleFlower(self))
            {
                self.bites--;
                self.room.PlaySound((self.bites == 0) ? SoundID.Slugcat_Eat_Karma_Flower : SoundID.Slugcat_Bite_Karma_Flower, self.firstChunk);
                self.firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);
                if (self.bites >= 1)
                {
                    return;
                }
                if (grasp.grabber is Player player)
                {
                    player.ObjectEaten(self);

                    if (player.SlugCatClass != WatcherEnums.SlugcatStatsName.Watcher && CWTs.PlayerCWT.TryGetData(player, out var data))
                    {
                        data.rippleMode = true;
                        data.startingRipple = true;
                        data.rippleTimer = rippleSpaceDuration;
                        Log.LogMessage("Ripple flower eaten, starting ripple sequence");
                    }
                    else
                    {
                        player.consumedRippleFood = 800;
                    }
                }
                grasp.Release();
                self.Destroy();
            }
            else orig(self, grasp, eu);
        }

        public static void KarmaFlower_DrawSprites(On.KarmaFlower.orig_DrawSprites orig, KarmaFlower self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (IsRippleFlower(self))
            {
                sLeaser.sprites[self.EffectSprite(0)].color = Color.Lerp(RainWorld.RippleColor, Color.white, 0.3f);
            }
        }

        public static void KarmaFlower_InitiateSprites(On.KarmaFlower.orig_InitiateSprites orig, KarmaFlower self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (IsRippleFlower(self))
            {
                sLeaser.sprites[self.StalkSprite].shader = RainWorld.TryGetRippleMaskedShaderVariant(self.abstractPhysicalObject.rippleLayer, "RippleBasic");
                sLeaser.sprites[self.RingSprite].shader = RainWorld.TryGetRippleMaskedShaderVariant(self.abstractPhysicalObject.rippleLayer, "RippleBasic");
                for (int i = 0; i < 4; i++)
                {
                    sLeaser.sprites[self.PetalSprite(i)].shader = RainWorld.TryGetRippleMaskedShaderVariant(self.abstractPhysicalObject.rippleLayer, "RippleBasic");
                }
                sLeaser.sprites[self.EffectSprite(2)].shader = RainWorld.TryGetRippleMaskedShaderVariant(self.abstractPhysicalObject.rippleLayer, "RippleGlow");
            }
        }

        public static void KarmaFlower_ApplyPalette(On.KarmaFlower.orig_ApplyPalette orig, KarmaFlower self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (IsRippleFlower(self))
            {
                self.color = RainWorld.RippleColor;
                self.stalkColor = Color.Lerp(palette.blackColor, palette.fogColor, 0.3f);
                return;
            }
            orig(self, sLeaser, rCam, palette);
        }

        private static void ExitRippleSpace(Player self, PlayerCWT.DataClass data)
        {
            Log.LogMessage("ExitRippleSpace called. isCamo: " + self.isCamo + " rippleLayer: " + self.abstractCreature.rippleLayer + " lastRippleState: " + (self.room?.game?.cameras?[0]?.lastRippleState.ToString() ?? "null"));

            self.Stun(12);
            data.startingRipple = false;
            data.activationTimer = 0;
            self.performingActivationTimer = 0;
            self.rippleActivating = false;
            self.startingCamoStateOnActivate = -1;

            if (self.isCamo)
            {
                Log.LogMessage("ExitRippleSpace calling ToggleCamo. rippleLevel: " + self.rippleLevel);
                self.ToggleCamo();
                Log.LogMessage("ExitRippleSpace after ToggleCamo. isCamo: " + self.isCamo + " lastRippleState: " + (self.room?.game?.cameras?[0]?.lastRippleState.ToString() ?? "null"));
            }
            else
            {
                Log.LogMessage("ExitRippleSpace skipping ToggleCamo, isCamo was already false");
            }

            data.rippleMode = false;
            self.abstractCreature.rippleLayer = 0;

            if (self.rippleData != null)
            {
                Log.LogMessage("ExitRippleSpace rippleData state. gameplayRippleActive: " + self.rippleData.gameplayRippleActive + " gameplayRippleAnimation: " + self.rippleData.gameplayRippleAnimation);
                self.rippleData.gameplayRippleActive = false;
                self.rippleData.gameplayRippleAnimation = 0f;
            }
            else
            {
                Log.LogMessage("ExitRippleSpace rippleData is null");
            }

            if (self.transitionRipple != null)
            {
                self.transitionRipple.Destroy();
                self.transitionRipple = null;
            }

            Log.LogMessage("ExitRippleSpace complete. rippleLayer: " + self.abstractCreature.rippleLayer + " lastRippleState: " + (self.room?.game?.cameras?[0]?.lastRippleState.ToString() ?? "null") + " gameplayRippleActive: " + (self.rippleData?.gameplayRippleActive.ToString() ?? "null"));
        }

        public static void RoomCamera_RefreshRippleMask(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            int matches = 0;
            while (c.TryGotoNext(MoveType.After,
                i => i.MatchLdfld<RippleCameraData>("gameplayRippleActive")))
            {
                matches++;
                if (matches == 2)
                {
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate<Func<bool, RoomCamera, bool>>((gameplayActive, cam) =>
                    {
                        Log.LogMessage("RefreshRippleMask IL check. gameplayActive: " + gameplayActive + " lastRippleState: " + cam.lastRippleState + " result: " + (gameplayActive && cam.lastRippleState));
                        return gameplayActive && cam.lastRippleState;
                    });
                    break;
                }
            }
            if (matches < 2)
            {
                Log.LogMessage("RefreshRippleMask IL hook FAILED to find second gameplayRippleActive read! matches found: " + matches);
            }
        }

        public static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (self?.room == null || self.inShortcut || !PlayerCWT.TryGetData(self, out var data) || !data.rippleMode)
            {
                return;
            }
            if (self.SlugCatClass == Watcher.WatcherEnums.SlugcatStatsName.Watcher)
            {
                return;
            }
            if (data.rippleTimer > 0 && self.standingInWarpPointProtectionTime < 10)
            {
                self.watcherInstability = (float)(data.rippleTimer) / (float)rippleSpaceDuration;
                self.watcherInstability /= 2;
                self.WatcherInstabilityUpdate();
                data.rippleTimer--;
                if (data.rippleTimer == 0)
                {
                    Log.LogMessage("Ripple timer reached 0, calling ExitRippleSpace");
                    ExitRippleSpace(self, data);
                    return;
                }
            }
            if (data.startingRipple)
            {
                Log.LogMessage("Timer: " + data.activationTimer);
                if (self.startingCamoStateOnActivate == -1)
                {
                    self.startingCamoStateOnActivate = (self.isCamo ? 1 : 0);
                    self.ringsToSpiralsTarget = self.startingCamoStateOnActivate;
                }
                if (self.transitionRipple == null)
                {
                    self.rippleAnimationJitterTimer = UnityEngine.Random.Range(0, 100);
                    self.rippleAnimationIntensityTarget = 0f;
                    self.transitionRipple = self.SpawnWatcherMechanicRipple();
                    self.transitionRipple.Data.scale = self.GetTransitionRippleTargets(5f).Item1;
                }
                data.activationTimer++;

                if (!self.CanLevitate)
                {
                    self.rippleActivating = true;
                }
                if (data.activationTimer == 80 && self.performingActivationTimer == 0)
                {
                    Log.LogMessage("Setting ripple layer! rippleLevel: " + self.rippleLevel);
                    self.ChangeRippleLayer(1);
                    if (self.rippleData != null)
                    {
                        self.rippleData.gameplayRippleActive = true;
                        self.rippleData.gameplayRippleAnimation = 1f;
                    }
                    data.startingRipple = false;
                    self.ToggleCamo();
                    Log.LogMessage("After entry ToggleCamo. isCamo: " + self.isCamo + " lastRippleState: " + (self.room?.game?.cameras?[0]?.lastRippleState.ToString() ?? "null"));
                }
                if (self.performingActivationTimer > 0)
                {
                    self.performingActivationTimer++;
                    if (self.performingActivationTimer >= self.performingActivationDuration)
                    {
                        self.performingActivationTimer = 0;
                    }
                }
                else if (data.activationTimer >= self.enterIntoCamoDuration)
                {
                    self.performingActivationTimer = 1;
                }
            }
            else
            {
                if (data.activationTimer > 0)
                {
                    data.activationTimer = 0;
                    self.performingActivationTimer = 0;
                    self.StopLevitation();
                }
                self.rippleActivating = false;
                self.startingCamoStateOnActivate = -1;
            }
            if (data.rippleMode)
            {
                if (self.room.game.cameras != null)
                {
                    self.CamoUpdate();
                }
                if (self.transitionRipple != null)
                {
                    self.TransitionRippleUpdate();
                }
                if (self.warpSpawningRipple != null)
                {
                    self.WarpSpawningUpdate();
                }
            }
        }

        public static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            sLeaser.sprites[0].shader = Custom.rainWorld.Shaders["PlayerCamoMaskBeforePlayer"];
            for (int i = 1; i < 10; i++)
            {
                sLeaser.sprites[i].shader = Custom.rainWorld.Shaders["RippleBasicBothSides"];
            }
            sLeaser.sprites[11].shader = Custom.rainWorld.Shaders["RippleBasicBothSides"];
        }

        public static float PlayerRippleLevel(Func<Player, float> orig, Player self)
        {
            if (PlayerCWT.TryGetData(self, out var data) && data.rippleMode)
            {
                return 5f;
            }
            return orig(self);
        }

        public static float PlayerMaxRippleLevel(Func<Player, float> orig, Player self)
        {
            if (PlayerCWT.TryGetData(self, out var data) && data.rippleMode)
            {
                return 5f;
            }
            return orig(self);
        }
    }
}