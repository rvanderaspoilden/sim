﻿using Sim.Building;
using Sim.Enums;
using UnityEngine;

namespace Sim.Scriptables {
    [CreateAssetMenu(fileName = "Paint", menuName = "Configurations/Paint")]
    public class PaintConfig : ScriptableObject {
        [SerializeField] private int id;
        [SerializeField] private string displayName;
        [SerializeField] private Material material;
        [SerializeField] private PropsConfig bucketPropsConfig;
        [SerializeField] private BuildSurfaceEnum surface;
        [SerializeField] private bool customColor;

        public int GetId() {
            return this.id;
        }
        
        public string GetDisplayName() {
            return this.displayName;
        }

        public Material GetMaterial() {
            return this.material;
        }

        public BuildSurfaceEnum GetSurface() {
            return this.surface;
        }

        public bool AllowCustomColor() {
            return this.customColor;
        }

        public PropsConfig GetBucketPropsConfig() {
            return this.bucketPropsConfig;
        }

        public bool IsWallCover() {
            return this.surface == BuildSurfaceEnum.WALL;
        }

        public bool IsGroundCover() {
            return this.surface == BuildSurfaceEnum.GROUND;
        }
    }   
}
