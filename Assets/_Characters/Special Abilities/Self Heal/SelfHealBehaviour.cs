﻿
namespace RPG.Characters
{
    public class SelfHealBehaviour : AbilityBehaviour
    {
        Player player = null;

        void Start()
        {
            player = GetComponent<Player>();
        }

        public override void Use(AbilityUseParams useParams)
        {
            player.Heal((config as SelfHealConfig).GetAmountToHeal());
            PlayParticleEffect();
            PlayAbilitySound();
        }
    }
}
