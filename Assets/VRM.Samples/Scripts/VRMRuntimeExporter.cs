using System.IO;
using UnityEngine;
using UnityEngine.UI;
using VRM;
using UniGLTF;
using UniJSON;

namespace VRM.Samples
{

    public class VRMRuntimeExporter : MonoBehaviour
    {
        private void Start()
        {
            var f = new GLTFJsonFormatter();

            Debug.Log("OK");
            try
            {
                f.Serialize("value");
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }

            UnityEngine.Application.Quit();
        }
    }
}
