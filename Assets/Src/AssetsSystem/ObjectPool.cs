using System.Collections.Generic;
namespace AssetSystem
{
    public class ObjectPool<T> where T : new()
    {
        public static Stack<T> _unityObjects = new Stack<T>();
        static T v;
        public static T CreateObject()
        {
            if (_unityObjects.Count > 0)
            {
                v = _unityObjects.Pop();
                return v;
            }
            else
            {
                return new T();
            }

        }

        public static void ReclaimObject(T item)
        {
            _unityObjects.Push(item);
            (item as ObjectPool<T>).Reset();
        }


        public virtual void Reset()
        {

        }
    }
}