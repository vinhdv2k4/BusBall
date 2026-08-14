using UnityEngine;

[CreateAssetMenu(fileName = "VfxSO", menuName = "Configs/VfxSO")]
public class VfxSO : ScriptableObject
{
    [SerializeField] private ParticleSystem vfxBoxDie;
    [SerializeField] private ParticleSystem vfxBusLargeLanding;
    [SerializeField] private ParticleSystem vfxBusMediumLanding;
    [SerializeField] private ParticleSystem vfxBusSmallLanding;
    [SerializeField] private ParticleSystem vfxBusReleaseBall;
    [SerializeField] private ParticleSystem vfxBusSmoke;
    [SerializeField] private ParticleSystem vfxHurryBusSmoke;
    [SerializeField] private ParticleSystem vfxBusHit;
    [SerializeField] private ParticleSystem vfxRevealBus;
    [SerializeField] private ParticleSystem vfxElectric;
    [SerializeField] private ParticleSystem vfxIceBreak;
    [SerializeField] private ParticleSystem vfxStuckBus;

    public ParticleSystem VfxBoxDie => vfxBoxDie;
    public ParticleSystem VfxBusLargeLanding => vfxBusLargeLanding;
    public ParticleSystem VfxBusMediumLanding => vfxBusMediumLanding;
    public ParticleSystem VfxBusSmallLanding => vfxBusSmallLanding;
    public ParticleSystem VfxBusReleaseBall => vfxBusReleaseBall;
    public ParticleSystem VfxBusSmoke => vfxBusSmoke;
    public ParticleSystem VfxHurryBusSmoke => vfxHurryBusSmoke;
    public ParticleSystem VfxBusHit => vfxBusHit;
    public ParticleSystem VfxRevealBus => vfxRevealBus;
    public ParticleSystem VfxStuckBus => vfxStuckBus;
    public ParticleSystem VfxElectric => vfxElectric;
    public ParticleSystem VfxIceBreak => vfxIceBreak;

    public ParticleSystem Get(VfxType type)
    {
        return type switch
        {
            VfxType.SmokeDefault => VfxBusSmoke,
            VfxType.SmokeHurryBus => VfxHurryBusSmoke,
            VfxType.BusHit => VfxBusHit,
            VfxType.BusReleaseBall => VfxBusReleaseBall,
            VfxType.BusSmallLanding => VfxBusSmallLanding,
            VfxType.BusMediumLanding => VfxBusMediumLanding,
            VfxType.BusLargeLanding => VfxBusLargeLanding,
            VfxType.BoxDie => VfxBoxDie,
            VfxType.RevealBus => VfxRevealBus,
            VfxType.StuckBus => VfxStuckBus,
            VfxType.Electric => VfxElectric,
            VfxType.IceBreak => VfxIceBreak,
            _ => null
        };
    }
}
