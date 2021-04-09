﻿using System.Linq;
using Mirror;
using Photon.Pun;
using Photon.Realtime;
using Sim.Enums;
using Sim.Interactables;
using Sim.Scriptables;
using UnityEngine;

namespace Sim.Building {
    public class PaintBucket : Props {
        [Header("Bucket Settings")]
        [SyncVar]
        [SerializeField]
        private Color color = Color.white;

        [Header("Bucket settings debug")]
        [SyncVar]
        [SerializeField]
        private int paintConfigId;

        private PaintConfig paintConfig;
        public delegate void OnOpen(PaintBucket bucketOpened);

        public static event OnOpen OnOpened;

        protected override void Execute(Action action) {
            if (action.Type.Equals(ActionTypeEnum.PAINT)) {
                OnOpened?.Invoke(this);
            }
        }

        [Server]
        public void Init(int paintId, float[] colorArray) {
            this.paintConfigId = paintId;
            
            if (colorArray != null && colorArray.Length >= 3) {
                this.color = new Color(colorArray[0], colorArray[1], colorArray[2]);
            }
        }

        public PaintConfig GetPaintConfig() {
            return this.paintConfig;
        }
        
        public Color GetColor() {
            return this.color;
        }
    }
}