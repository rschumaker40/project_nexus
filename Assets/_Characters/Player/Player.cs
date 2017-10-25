﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RPG.CameraUI; // TODO consider rewiring
using RPG.Core;
using RPG.Weapons;

namespace RPG.Characters
{
    public class Player : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHealthPoints = 100f;
        [SerializeField] float baseDamage = 10;
        [SerializeField] Weapon weaponInUse = null;
        [SerializeField] AnimatorOverrideController animatorOverrideController = null;
        [SerializeField] SpecialAbility[] abilities;
        [SerializeField] AudioClip[] damageSounds;
        [SerializeField] AudioClip[] deathSounds;

        const string DEATH_TRIGGER = "Death";
        const string ATTACK_TRIGGER = "Attack";

        AudioSource audioSource;
        Animator animator;
        float currentHealthPoints;
        CameraRaycaster cameraRaycaster;
        float lastHitTime = 0f;
        float weaponDamage;
        bool playedDamageSoundRecently = false;

        public float healthAsPercentage
        {
            get { return currentHealthPoints / maxHealthPoints; }
        }

        void Start()
        {
            RegisterForMouseClick();
            SetCurrentMaxHealth();
            PutWeaponInHand();
            SetupRuntimeAnimator();
            abilities[0].AttachComponentTo(gameObject);
            audioSource = GetComponent<AudioSource>();
        }

        // Damage interface
        public void AdjustHealth(float amountChanged)
        {
            bool playerDies = (currentHealthPoints - amountChanged <= 0);
            ReduceHealth(amountChanged);            
            if (!playedDamageSoundRecently)
            {
                StartCoroutine(PlayDamageSounds());
            }            
            if (playerDies)
            {
                StopCoroutine(PlayDamageSounds());
                StartCoroutine(KillPlayer());
            }
        }

        IEnumerator PlayDamageSounds()
        {
            playedDamageSoundRecently = true;
            audioSource.clip = damageSounds[UnityEngine.Random.Range(0, damageSounds.Length)];
            audioSource.Play();
            yield return new WaitForSecondsRealtime(3f);
            playedDamageSoundRecently = false;
        }

        IEnumerator KillPlayer()
        {
            animator.SetTrigger(DEATH_TRIGGER);
            audioSource.clip = deathSounds[UnityEngine.Random.Range(0, deathSounds.Length)];
            audioSource.Play();
            yield return new WaitForSecondsRealtime(audioSource.clip.length);
            SceneManager.LoadScene(0);
        }

        private void ReduceHealth(float damage)
        {
            currentHealthPoints = Mathf.Clamp(currentHealthPoints - damage, 0f, maxHealthPoints);
            // play hit sound and animations
        }

        private void SetCurrentMaxHealth()
        {
            currentHealthPoints = maxHealthPoints;
        }

        private void SetupRuntimeAnimator()
        {
            animator = GetComponent<Animator>();
            animator.runtimeAnimatorController = animatorOverrideController;
            animatorOverrideController["DEFAULT ATTACK"] = weaponInUse.GetAttackAnimClip(); // TODO Remove const
            animatorOverrideController["HumanoidIdle"] = weaponInUse.GetIdleAnimClip();
            animatorOverrideController["HumanoidRun"] = weaponInUse.GetRunAnimClip();
        }

        private void PutWeaponInHand()
        {
            var weaponPrefab = weaponInUse.GetWeaponPrefab();
            GameObject dominantHand = RequestDominantHand();
            var weapon = Instantiate(weaponPrefab, dominantHand.transform);
            weapon.transform.localPosition = weaponInUse.gripTransform.localPosition;
            weapon.transform.localRotation = weaponInUse.gripTransform.localRotation;
        }

        // TODO Remove old dominant hand code once my RequestDominantHand method is thoroughly tested
        // Old Dominant Hand script. Leaving in case my code breaks I have reference
        /*private GameObject RequestDominantHand()
        {
            var dominantHands = GetComponentsInChildren<DominantHand>();
            int numberOfDominantHands = dominantHands.Length;
            Assert.IsFalse(numberOfDominantHands <= 0, "No dominant hand found on player. Please add one.");
            Assert.IsFalse(numberOfDominantHands > 1, "Multiple dominant hand scripts on player. Please remove one.");
            return dominantHands[0].gameObject;
        }*/

        private GameObject RequestDominantHand()
        {
            var handed = weaponInUse.GetDominantGrip();
            if (handed == Weapon.DominantGripHand.RightHand)
            {
                var dominantHands = GetComponentsInChildren<DominantHandRight>();
                return dominantHands[0].gameObject;
            }
            if (handed == Weapon.DominantGripHand.LeftHand)
            {
                var dominantHands = GetComponentsInChildren<DominantHandLeft>();
                return dominantHands[0].gameObject;
            }
            return null;             
        }
        
        private void RegisterForMouseClick()
        {
            cameraRaycaster = FindObjectOfType<CameraRaycaster>();
            cameraRaycaster.onMouseOverEnemy += OnMouseOverEnemy;
        }

        void OnMouseOverEnemy(Enemy enemy)
        {
            if (Input.GetMouseButton(0) && IsTargetInRange(enemy.gameObject))
            {
                AttackTarget(enemy);
            }
            else if (Input.GetMouseButtonDown(1))
            {
                AttemptSpecialAbility(0, enemy);
            }
        }

        private void AttemptSpecialAbility(int abilityIndex, Enemy enemy)
        {
            var energyComponent = GetComponent<Energy>();
            var energyCost = abilities[abilityIndex].GetEnergyCost();
            weaponDamage = weaponInUse.GetDamagePerHit();
            if (energyComponent.IsEnergyAvailable(energyCost))
            {
                energyComponent.ConsumeEnergy(energyCost);
                var abilityParams = new AbilityUseParams(enemy, baseDamage, weaponDamage);
                abilities[abilityIndex].Use(abilityParams);
            }
        }

        private void AttackTarget(Enemy enemy)
        {
            if (Time.time - lastHitTime > weaponInUse.GetMinTimeBetweenHits())
            {
                animator.SetTrigger(ATTACK_TRIGGER);
                enemy.AdjustHealth(baseDamage + weaponInUse.GetDamagePerHit());
                print("Normal Attack. Damage Dealt :" + (baseDamage + weaponInUse.GetDamagePerHit()));            
                lastHitTime = Time.time;
            }
        }

        private bool IsTargetInRange(GameObject target)
        {
            float distanceToTarget = (target.transform.position - transform.position).magnitude;
            return distanceToTarget <= weaponInUse.GetAttackRange();
        }
    }
}

