using Fisobs.Core;
using lsfUtils.Items.PoisonDart;
using System;
using System.Globalization;
using static lsfUtils.Plugin;
using static lsfUtils.Enums;
using lsfUtils.Items;

namespace lsfUtils.Items.Dart
{
    public class AbstractDart : AbstractPhysicalObject
    {
        public float poison;
        public float poisonHue;

        public DartType dartType;

        public AbstractDart(World world, Dart realizedObject, WorldCoordinate pos, EntityID ID) : base(world, AbstractPhysicalObjectType.Dart, realizedObject, pos, ID)
        {

        }

        public override void Realize()
        {
            base.Realize();
            if (realizedObject == null)
            {
                if (dartType == DartType.Default)
                {
                    realizedObject = new Dart(this);
                }
                realizedObject = new PoisonDart.PoisonDart(this, poison);
            }
        }

        public override string ToString()
        {
            return this.SaveToString($"{poison},{poisonHue},{dartType}");
        }
    }
}