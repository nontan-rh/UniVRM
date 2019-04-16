using System.Collections.Generic;
using UniJSON;


namespace UniGLTF
{
    public class GLTFJsonFormatter: UniJSON.JsonFormatter
    {
        public void GLTFValue(JsonSerializableBase s)
        {
        }

        public void GLTFValue<T>(IEnumerable<T> values) where T : JsonSerializableBase
        {
        }

        public void GLTFValue(List<string> values)
        {
        }
    }
}
