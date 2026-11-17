
public class Heart : Hazard
{
    protected override float LifeTime => Items.pickupLifetime;

    protected override void OnHitPlayer(Health player)
    {
        player.Heal(Items.heartHeal);
    }
}
