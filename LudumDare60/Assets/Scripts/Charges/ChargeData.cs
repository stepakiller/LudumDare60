using UnityEngine;

[CreateAssetMenu(menuName = "LudumDare60/Charge", fileName = "Charge_")]
public class ChargeData : ScriptableObject
{
    public string chargeName;
    [TextArea(3, 10)] public string description;
    public Sprite icon;
    public Sprite diagram;
    public GameObject rocketPrefab;
}
