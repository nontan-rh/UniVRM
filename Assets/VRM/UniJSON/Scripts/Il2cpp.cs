namespace UniJSON
{
    public class Il2cpp
    {
        public static void TellIl2cppToGenerateGenericMethodCode<T>()
        {
            var f = new JsonFormatter();
            f.Serialize(default(T));
            f.Serialize(default(T[]));
            f.SerializeArray(default(System.Collections.Generic.IEnumerable<T>));
            f.SerializeArray(default(System.Collections.Generic.IEnumerable<T[]>));

            GenericDeserializer<UniJSON.JsonValue, T[]>.GenericArrayDeserializer<T>(new ListTreeNode<UniJSON.JsonValue>());
            GenericDeserializer<UniJSON.JsonValue, System.Collections.Generic.List<T>>.GenericListDeserializer<T>(new ListTreeNode<UniJSON.JsonValue>());

            JsonArrayValidator.TellIl2cppToGenerateGenericMethodCode<T>();

            TellIl2cppToGenerateNestedGenericMethodCode<T, System.Boolean>();
            TellIl2cppToGenerateNestedGenericMethodCode<T, System.Byte>();
            TellIl2cppToGenerateNestedGenericMethodCode<T, System.SByte>();
            TellIl2cppToGenerateNestedGenericMethodCode<T, System.Char>();
            TellIl2cppToGenerateNestedGenericMethodCode<T, System.Decimal>();
            TellIl2cppToGenerateNestedGenericMethodCode<T, System.Double>();
            TellIl2cppToGenerateNestedGenericMethodCode<T, System.Single>();
            TellIl2cppToGenerateNestedGenericMethodCode<T, System.Int32>();
            TellIl2cppToGenerateNestedGenericMethodCode<T, System.UInt32>();
            TellIl2cppToGenerateNestedGenericMethodCode<T, System.Int64>();
            TellIl2cppToGenerateNestedGenericMethodCode<T, System.UInt64>();
            TellIl2cppToGenerateNestedGenericMethodCode<T, System.Object>();
            TellIl2cppToGenerateNestedGenericMethodCode<T, System.Int16>();
            TellIl2cppToGenerateNestedGenericMethodCode<T, System.UInt16>();
            TellIl2cppToGenerateNestedGenericMethodCode<T, System.String>();

            TellIl2cppToGenerateNestedGenericMethodCode<T, UnityEngine.Color>();
            TellIl2cppToGenerateNestedGenericMethodCode<T, UnityEngine.Color32>();
            TellIl2cppToGenerateNestedGenericMethodCode<T, UnityEngine.Rect>();
            TellIl2cppToGenerateNestedGenericMethodCode<T, UnityEngine.RectOffset>();
            TellIl2cppToGenerateNestedGenericMethodCode<T, UnityEngine.Vector2>();
            TellIl2cppToGenerateNestedGenericMethodCode<T, UnityEngine.Vector3>();
            TellIl2cppToGenerateNestedGenericMethodCode<T, UnityEngine.Vector4>();
#if UNITY_2017_2_OR_NEWER
            TellIl2cppToGenerateNestedGenericMethodCode<T, UnityEngine.RectInt>();
            TellIl2cppToGenerateNestedGenericMethodCode<T, UnityEngine.Vector2Int>();
            TellIl2cppToGenerateNestedGenericMethodCode<T, UnityEngine.Vector3Int>();
#endif
        }

        public static void TellIl2cppToGenerateNestedGenericMethodCode<T, U>()
        {
            TellIl2cppToGenerateGenericMethodCode<T>();
            TellIl2cppToGenerateGenericMethodCode<U>();
            JsonObjectValidator.TellIl2cppToGenerateGenericMethodCode<T, U>();
        }

        private static void TellIl2cppToGenerateDefaultGenericMethodCode()
        {
            TellIl2cppToGenerateGenericMethodCode<System.Object>();
        }
    }
}
