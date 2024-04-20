using UnityEngine;

namespace WizardsCode.Test
{
    [CreateAssetMenu(fileName = "TestScriptableObject", menuName = "Wizards Code/Data Manager/Test Scriptable Object", order = 1)]
    public class TestScriptableObject : ScriptableObject
    {
        [SerializeField, Tooltip("A displaynae for the object")]
        private string displayName = "Test Scriptable Object";
    }
}
