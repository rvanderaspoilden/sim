﻿using System.Collections.Generic;
using System.Linq;
using Sim.Building;
using Sim.Enums;
using Sim.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sim {
    public class CameraManager : MonoBehaviour {
        [Header("Settings")]
        [SerializeField]
        private LayerMask layerMaskInFreeMode;

        private new Camera camera;

        private List<PropsRenderer> displayedPropsRenderers;

        private BuildCamera buildCamera;

        private ThirdPersonCamera tpsCamera;

        private float lastCameraPosition;

        private RaycastHit hit;

        private bool forceWallHidden;

        private CameraModeEnum currentMode;

        public static CameraManager Instance;

        private void Awake() {
            if (Instance != null) {
                Destroy(this.gameObject);
            }

            Instance = this;

            this.camera = GetComponentInChildren<Camera>();
            this.buildCamera = GetComponent<BuildCamera>();
            this.tpsCamera = GetComponent<ThirdPersonCamera>();
            this.displayedPropsRenderers = new List<PropsRenderer>();

            this.buildCamera.enabled = false;
            this.tpsCamera.enabled = false;
        }

        private void Start() {
            BuildPreviewPanelUI.OnToggleHideProps += TogglePropsVisible;
            BuildPreviewPanelUI.OnToggleHideWalls += ToggleWallVisible;

            Player.OnStateChanged += OnStateChanged;
        }

        private void OnDestroy() {
            BuildPreviewPanelUI.OnToggleHideProps -= TogglePropsVisible;
            BuildPreviewPanelUI.OnToggleHideWalls -= ToggleWallVisible;

            Player.OnStateChanged -= OnStateChanged;
        }

        void Update() {
            //this.ManageWorldTransparency();

            if (Input.GetKeyDown(KeyCode.H) && this.currentMode == CameraModeEnum.FREE) {
                this.ToggleWallVisible(!this.forceWallHidden);
            }

            if (this.currentMode == CameraModeEnum.FREE && !EventSystem.current.IsPointerOverGameObject()) {
                this.ManageInteraction();
            }
        }

        /*public void ManageWorldTransparency() {
            if (!RoomManager.LocalPlayer || this.currentOpenedBucket || (this.currentPropSelected && this.currentPropSelected.IsWallProps())) {
                return;
            }

            this.lastCameraPosition = this.camera.transform.position.magnitude;

            Vector3 dir = -(this.camera.transform.position -
                            (this.currentPropSelected ? this.currentPropSelected.transform.position : RoomManager.LocalPlayer.transform.position));
            Debug.DrawRay(this.camera.transform.position, dir, Color.blue);

            RaycastHit[] hits = Physics.RaycastAll(this.camera.transform.position, dir, 20, (1 << 10 | 1 << 12));
            if (hits.Length > 0) {
                List<PropsRenderer> hiddenObject = new List<PropsRenderer>();

                foreach (RaycastHit wallHit in hits) {
                    hiddenObject.AddRange(this.HidePropsNear(wallHit.point));
                }

                this.ResetRendererForPropsNotIn(hiddenObject);
            } else if (this.displayedPropsRenderers.Count > 0) // If camera doesn't hit wall so reset all hidden props
            {
                this.displayedPropsRenderers.ForEach(propsRenderer => propsRenderer.SetState(VisibilityStateEnum.SHOW));
                this.displayedPropsRenderers.Clear();
            }
        }*/

        private void TogglePropsVisible(bool hide) {
            FindObjectsOfType<Props>().ToList().Where(x => x.GetType() != typeof(Wall)).Select(x => x.GetComponent<PropsRenderer>()).ToList().ForEach(
                propsRenderer => {
                    if (propsRenderer && propsRenderer.IsHideable()) {
                        propsRenderer.SetVisibilityMode(hide ? VisibilityModeEnum.FORCE_HIDE : VisibilityModeEnum.AUTO);
                    }
                });
        }

        private void ToggleWallVisible(bool hide) {
            this.forceWallHidden = hide;

            FindObjectsOfType<Wall>().ToList().Where(x => !x.IsExteriorWall()).Select(x => x.GetComponent<PropsRenderer>()).ToList().ForEach(propsRenderer => {
                if (propsRenderer) {
                    propsRenderer.SetVisibilityMode(this.forceWallHidden ? VisibilityModeEnum.FORCE_HIDE : VisibilityModeEnum.AUTO);
                }
            });
        }

        private void OnStateChanged(Player player, StateType state) {
            if (player == RoomManager.LocalPlayer) {
                if (state == StateType.FREE) {
                    this.SetCurrentMode(CameraModeEnum.FREE);
                } else {
                    this.SetCurrentMode(CameraModeEnum.BUILD);
                }
            }
        }

        private List<PropsRenderer> HidePropsNear(Vector3 pos) {
            List<PropsRenderer> objectsToHide = Physics.OverlapSphere(pos, 3f)
                .ToList()
                .Select(x => x.GetComponentInParent<PropsRenderer>())
                .ToList();

            objectsToHide.ForEach(propsRenderer => {
                if (propsRenderer && propsRenderer.CanInteractWithCameraDistance()) {
                    // prevent NPE due to get componentInParent
                    propsRenderer.SetState(VisibilityStateEnum.HIDE);

                    this.displayedPropsRenderers.Add(propsRenderer);
                }
            });

            return objectsToHide;
        }

        private void ResetRendererForPropsNotIn(List<PropsRenderer> objectHidden) {
            // Reset objects which aren't in view area
            IEnumerable<PropsRenderer> difference = this.displayedPropsRenderers.Except(objectHidden);

            foreach (PropsRenderer propsRenderer in difference) {
                propsRenderer.SetState(VisibilityStateEnum.SHOW);
            }
        }

        private void SetCurrentMode(CameraModeEnum mode) {
            this.currentMode = mode;
            this.buildCamera.enabled = mode == CameraModeEnum.BUILD;
            this.tpsCamera.enabled = mode == CameraModeEnum.FREE;

            if (this.currentMode == CameraModeEnum.BUILD) {
                this.buildCamera.Setup(this.tpsCamera.GetVirtualCamera());
            }
        }

        private void ManageInteraction() {
            if (this.currentMode == CameraModeEnum.FREE && Physics.Raycast(this.camera.ScreenPointToRay(Input.mousePosition), out hit, 100, this.layerMaskInFreeMode)) {
                Props objectToInteract = hit.collider.GetComponentInParent<Props>();

                if (objectToInteract) {
                    if (objectToInteract.IsBuilt()) {
                        CursorManager.Instance.SetCursor(objectToInteract.GetConfiguration().GetCursor());
                    } else {
                        CursorManager.Instance.SetBuildCursor();
                    }


                    if (Input.GetMouseButtonDown(0)) {
                        bool canInteract = RoomManager.LocalPlayer && RoomManager.LocalPlayer.CanInteractWith(objectToInteract, hit.point);

                        if (canInteract) {
                            HUDManager.Instance.DisplayContextMenu(true, Input.mousePosition, objectToInteract);
                        } else {
                            RoomManager.Instance.MovePlayerTo(hit.point);
                            HUDManager.Instance.DisplayContextMenu(false, Vector3.zero);
                        }
                    }
                }
            } else if (CursorManager.Instance.GetCursor()) {
                CursorManager.Instance.SetCursor(null);
            }
        }
    }
}