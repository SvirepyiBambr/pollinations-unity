// Compile-check stubs for Unity engine types used by the runtime.
// Lives in a hidden Stubs~ folder that Unity ignores; used only by CI
// to syntax- and semantics-check Runtime/*.cs with Mono's mcs.
using System;
using System.Collections;

namespace UnityEngine
{
    public class Object
    {
        public static void Destroy(Object obj) { }
        public string name { get; set; }
    }

    public class Component : Object { }

    public class Behaviour : Component
    {
        public bool enabled { get; set; }
    }

    public class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(IEnumerator routine) => null;
    }

    public class Coroutine { }

    [Serializable]
    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() =>
            new T();
    }

    public struct Color
    {
        public float r, g, b, a;
    }

    public enum TextureFormat { RGBA32, RGB24 }

    public class Texture2D : Object
    {
        public Texture2D(int width, int height, TextureFormat format, bool mipChain) { }
        public bool LoadImage(byte[] data) => true;
    }
}

namespace UnityEngine.Networking
{
    public enum UnityWebRequestResult
    {
        Success,
        ConnectionError,
        ProtocolError,
        DataProcessingError,
    }

    public class UnityWebRequest : System.IDisposable
    {
        public UnityWebRequest(string url, string method = "GET") { }

        public string method { get; set; }
        public string url { get; set; }
        public string error { get; private set; }
        public UnityWebRequestResult result { get; private set; }
        public long responseCode { get; private set; }
        public UploadHandler uploadHandler { get; set; }
        public DownloadHandler downloadHandler { get; set; }

        public void SetRequestHeader(string name, string value) { }
        public UnityWebRequestAsyncOperation SendWebRequest() => null;
        public void Dispose() { }

        public static UnityWebRequest Get(string url) => new UnityWebRequest(url);
    }

    public class AsyncOperation
    {
        public event System.Action<AsyncOperation> completed;
        internal void Finish() => completed?.Invoke(this);
    }

    public class UnityWebRequestAsyncOperation : AsyncOperation { }

    public class UploadHandlerRaw : UploadHandler
    {
        public UploadHandlerRaw(byte[] data) { }
    }

    public abstract class UploadHandler { }

    public abstract class DownloadHandler
    {
        public string text { get; protected set; }
        public byte[] data { get; protected set; }
    }

    public class DownloadHandlerBuffer : DownloadHandler { }
}
