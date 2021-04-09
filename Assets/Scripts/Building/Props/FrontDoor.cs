using Mirror;
using Sim.Building;
using TMPro;
using UnityEngine;

public class FrontDoor : SimpleDoor {

    [SerializeField]
    private TextMeshPro numberTxt;
    
    [Header("Debug")]
    [SyncVar]
    [SerializeField]
    private int number;

    public int Number {
        get => number;
        set {
            number = value;
            this.numberTxt.text = number.ToString();
        }
    }
}
