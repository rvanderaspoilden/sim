﻿using System.Collections.Generic;
using System.Linq;
using Mirror;
using Sim.Enums;
using Sim.Scriptables;
using UnityEngine;

namespace Sim {
    public class DatabaseManager : MonoBehaviour {
        [Header("Settings")]
        [SerializeField]
        private Material transparentMaterial;

        [SerializeField]
        private Material unbuiltMaterial;

        [SerializeField]
        private Material errorMaterial;

        public static PropsDatabaseConfig PropsDatabase;
        public static PaintDatabaseConfig PaintDatabase;
        public static List<MoodConfig> MoodConfigs;
        public static List<ShopCategoryConfig> ShopCategoryConfigs;

        public static DatabaseManager Instance;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(this.gameObject);
            } else {
                Instance = this;
            }

            PropsDatabase = Resources.Load<PropsDatabaseConfig>("Configurations/Databases/Props Database");
            Debug.Log("Props database loaded");

            PaintDatabase = Resources.Load<PaintDatabaseConfig>("Configurations/Databases/Paint Database");
            Debug.Log("Paint database loaded");

            MoodConfigs = Resources.LoadAll<MoodConfig>("Configurations/Moods").ToList();
            Debug.Log("Mood Configs loaded : " + MoodConfigs.Count);
            
            ShopCategoryConfigs = Resources.LoadAll<ShopCategoryConfig>("Configurations/Shop/Categories").ToList();
            Debug.Log("Shop Category Configs loaded : " + ShopCategoryConfigs.Count);

            RegisterPrefabs();

            DontDestroyOnLoad(this.gameObject);
        }

        private static void RegisterPrefabs() {
            foreach (PropsConfig propsConfig in PropsDatabase.GetProps()) {
                NetworkManager.singleton.spawnPrefabs.Add(propsConfig.GetPrefab().gameObject);
            }
        }

        public static MoodConfig GetMoodConfigByEnum(MoodEnum moodEnum) {
            return MoodConfigs.Find(config => config.MoodEnum == moodEnum);
        }

        public static ShopCategoryConfig GetShopCategoryByPropsType(PropsType propsType) {
            return ShopCategoryConfigs.Find(config => config.PropsType == propsType);
        }

        public Material GetTransparentMaterial() {
            return this.transparentMaterial;
        }

        public Material GetUnbuiltMaterial() {
            return this.unbuiltMaterial;
        }

        public Material GetErrorMaterial() {
            return this.errorMaterial;
        }
    }
}