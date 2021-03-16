﻿using System.Linq;
using Photon.Pun;
using Sim.Building;
using Sim.Enums;
using Sim.Interactables;
using Sim.Scriptables;
using Sim.UI;
using UnityEngine;

namespace Sim {
    [RequireComponent(typeof(Character))]
    public class PlayerInteraction : MonoBehaviourPun {
        [Header("DEBUG")]
        private Package currentOpenedPackage;

        private PropsConfig propsToPackage;

        private PaintBucket currentOpenedBucket;

        private PaintConfig paintToPackage;

        private Character character;

        private void Awake() {
            this.character = GetComponent<Character>();
        }

        private void Start() {
            if (!photonView.IsMine) Destroy(this);

            AliDiscountCatalogUI.OnPropsClicked += OnSelectPropsFromAdminPanel;
            AliDiscountCatalogUI.OnPaintClicked += OnSelectPaintFromAdminPanel;
            BuildManager.OnCancel += OnBuildModificationCanceled;
            BuildManager.OnValidatePropCreation += OnValidatePropCreation;
            BuildManager.OnValidatePropEdit += OnValidatePropEdit;
            BuildManager.OnValidatePaintModification += OnValidatePaintModification;
            Props.OnMoveRequest += OnMoveRequest;
            Package.OnOpened += OpenPackage;
            PaintBucket.OnOpened += OpenBucket;

            this.character.SetState(StateType.FREE);
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.F) && this.character.GetState() == StateType.FREE && PhotonNetwork.IsMasterClient && ApartmentManager.Instance &&
                ApartmentManager.Instance.IsOwner(NetworkManager.Instance.CharacterData)) {
                HUDManager.Instance.DisplayAdminPanel(true);
            }
        }

        private void OnDestroy() {
            AliDiscountCatalogUI.OnPropsClicked -= OnSelectPropsFromAdminPanel;
            AliDiscountCatalogUI.OnPaintClicked -= OnSelectPaintFromAdminPanel;
            BuildManager.OnCancel -= OnBuildModificationCanceled;
            BuildManager.OnValidatePropCreation -= OnValidatePropCreation;
            BuildManager.OnValidatePropEdit -= OnValidatePropEdit;
            BuildManager.OnValidatePaintModification -= OnValidatePaintModification;
            Props.OnMoveRequest -= OnMoveRequest;
            Package.OnOpened -= OpenPackage;
            PaintBucket.OnOpened -= OpenBucket;
        }

        private void OnMoveRequest(Props props) {
            this.character.SetState(StateType.MOVING_PROPS);

            BuildManager.Instance.Edit(props);
        }

        /**
         * Called when props was chosen from admin panel
         */
        private void OnSelectPropsFromAdminPanel(PropsConfig propsConfig) {
            this.propsToPackage = propsConfig;

            this.character.SetState(StateType.PACKAGING);

            BuildManager.Instance.Init(this.propsToPackage.GetPackageConfig());
        }

        /**
         * Called when paint was chosen from admin panel
         */
        private void OnSelectPaintFromAdminPanel(PaintConfig paintConfig) {
            this.paintToPackage = paintConfig;

            this.character.SetState(StateType.PACKAGING);

            BuildManager.Instance.Init(this.paintToPackage.GetBucketPropsConfig());
        }

        private void OpenPackage(Package package) {
            this.currentOpenedPackage = package;

            this.character.SetState(StateType.UNPACKAGING);

            BuildManager.Instance.Init(package.GetPropsConfigInside());
        }

        private void OpenBucket(PaintBucket bucket) {
            this.currentOpenedBucket = bucket;

            this.character.SetState(StateType.PAINTING);

            if (this.currentOpenedBucket.GetPaintConfig().IsWallCover()) {
                RoomManager.Instance.SetWallVisibility(VisibilityModeEnum.FORCE_SHOW);
            }

            BuildManager.Instance.Init(this.currentOpenedBucket);
        }

        private void OnBuildModificationCanceled() {
            this.currentOpenedPackage = null;
            this.currentOpenedBucket = null;
            this.propsToPackage = null;
            this.paintToPackage = null;
            this.character.SetState(StateType.FREE);
        }

        private void OnValidatePaintModification() {
            if (this.currentOpenedBucket.GetPaintConfig().IsWallCover()) {
                RoomManager.Instance.SetWallVisibility(VisibilityModeEnum.AUTO);

                FindObjectsOfType<Wall>().ToList().Where(x => x.IsPreview()).ToList().ForEach(x => x.ApplyModification());
            } else if (this.currentOpenedBucket.GetPaintConfig().IsGroundCover()) {
                FindObjectsOfType<Ground>().ToList().Where(x => x.IsPreview()).ToList().ForEach(x => x.ApplyModification());
            }

            PropsManager.Instance.DestroyProps(this.currentOpenedBucket, true);

            RoomManager.Instance.SaveRoom();

            this.character.SetState(StateType.FREE);
        }

        private void OnValidatePropCreation(PropsConfig propsConfig, Vector3 position, Quaternion rotation) {
            Props props = PropsManager.Instance.InstantiateProps(propsConfig, position, rotation, true);

            // Manage packaging for props
            if (this.character.GetState() == StateType.PACKAGING && this.propsToPackage) {
                props.SetIsBuilt(true);
                props.GetComponent<Package>().SetPropsInside(this.propsToPackage.GetId(), RpcTarget.All);
                this.propsToPackage = null;
            }

            // Manage packaging for paint
            if (this.character.GetState() == StateType.PACKAGING && this.paintToPackage) {
                props.SetIsBuilt(true);
                props.GetComponent<PaintBucket>().SetPaintConfigId(this.paintToPackage.GetId(), RpcTarget.All);
                this.paintToPackage = null;
            }

            // Manage unpackaging
            if (this.character.GetState() == StateType.UNPACKAGING && this.currentOpenedPackage) {
                props.SetIsBuilt(!propsConfig.MustBeBuilt());
                PropsManager.Instance.DestroyProps(this.currentOpenedPackage, true);
                this.currentOpenedPackage = null;
            }

            RoomManager.Instance.SaveRoom();

            // this.SwitchToFreeMode(); // TODO Camera manager must subscribe to this event to come back to free camera
            this.character.SetState(StateType.FREE);
        }

        private void OnValidatePropEdit(Props props) {
            props.UpdateTransform();

            RoomManager.Instance.SaveRoom();

            // this.SwitchToFreeMode(); // TODO Camera manager must subscribe to this event to come back to free camera
            this.character.SetState(StateType.FREE);
        }
    }
}