﻿using System;
using UnityEngine;

namespace RPG.Characters
{
    public class PowerAttackBehaviour : MonoBehaviour, ISpecialAbility
    {
        PowerAttackConfig config;

        public void SetConfig(PowerAttackConfig configToSet)
        {
            this.config = configToSet;
        }

        public void Use(AbilityUseParams useParams)
        {
            DealDamage(useParams);
            PlayParticleEffect();
        }

        private void PlayParticleEffect()
        {
            var prefab = Instantiate(config.GetParticlePrefab(), transform.position, Quaternion.identity);
            // TODO Decide if particle system attaches to player
            ParticleSystem myParticleSystem = prefab.GetComponent<ParticleSystem>();
            myParticleSystem.Play();
            Destroy(prefab, myParticleSystem.main.duration);
        }

        private void DealDamage(AbilityUseParams useParams)
        {
            print("base damage: " + useParams.baseDamage + ". weapon damage: " + useParams.weaponDamage + ".");
            float damageToDeal = ((useParams.baseDamage + useParams.weaponDamage) * config.GetExtraDamage());
            print("Power Attack. Damage Dealt: " + damageToDeal);
            useParams.target.AdjustHealth(damageToDeal);
        }
    }
}

