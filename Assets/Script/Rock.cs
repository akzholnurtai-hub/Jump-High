
public class Rock : Hazard
{
    protected override int Damage => Items.rockDamage;
    protected override float LifeTime => Items.pickupLifetime;
}
