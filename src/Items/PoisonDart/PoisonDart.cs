using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using lsfUtils.Items.Dart;
using MoreSlugcats;
using Noise;
using RWCustom;
using Smoke;
using UnityEngine;
using static lsfUtils.Plugin;

namespace lsfUtils.Items.PoisonDart
{
    public class PoisonDart : Dart.Dart
    {
        public static readonly float poisonPrecentagePerTick = 0.005f;

        public float remainingPoison;
        public int pullOutCounter;
        public int pullOutAttempts;

        public AbstractCreature shotFrom;
        public static float Rand => Random.value;

        public PoisonDart(AbstractDart abstr, float remainingPoison) : base(abstr)
        {
            this.remainingPoison = remainingPoison;
            Log.LogMessage("Remaining poison from ctor: " +  this.remainingPoison);
            pullOutAttempts = 0;
            pullOutCounter = 0;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (this.remainingPoison > 0f)
            {
                if (mode == Mode.StuckInCreature && stuckInObject != null)
                {
                    if (stuckInObject is Creature creature)
                    {
                        creature.InjectPoison(poisonPrecentagePerTick, Enums.Colors.PoisonColor);
                        this.remainingPoison = Mathf.Max(this.remainingPoison - poisonPrecentagePerTick, 0f);
                    }
                }
            }
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            sLeaser.sprites[0].color = Color.red;
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            Color waterShineColor = palette.waterShineColor;
            Color blackColor = palette.blackColor;
            color = Color.Lerp(waterShineColor, blackColor, 0.6f);
            sLeaser.sprites[0].color = Color.green;
        }

        public override void ChangeMode(Mode newMode)
        {
            if (newMode == Mode.StuckInCreature)
            {
                pullOutCounter = 120;
                pullOutAttempts = 0;
            }
            base.ChangeMode(newMode);
        }
    }
}
