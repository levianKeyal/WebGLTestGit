using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SextantDragRule : MonoBehaviour
{
    public int sextant;                   // Sextant ID (1–6)
    public List<int> allowedSextants;     // Allowed sextants to drag into
}
