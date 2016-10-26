using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PoolManager : MonoBehaviour {

	Dictionary<int,Queue<ObjectInstance>> poolDictionary = new Dictionary<int, Queue<ObjectInstance>> ();

	static PoolManager _instance;

	public static PoolManager instance {
		get {
			if (_instance == null) {
				_instance = FindObjectOfType<PoolManager>();
			}
			return _instance;
		}
	}

	public GameObject[] CreatePool(GameObject prefab, int poolSize) {
		int poolKey = prefab.GetInstanceID ();
		GameObject[] objects = new GameObject[poolSize];

		GameObject poolHolder = new GameObject(prefab.name + "pool");
		poolHolder.transform.parent = transform;

		if (!poolDictionary.ContainsKey (poolKey)) {
			poolDictionary.Add(poolKey, new Queue<ObjectInstance>());

			for(int i = 0; i < poolSize; i++) {
				GameObject newGameObject = Instantiate(prefab) as GameObject;
				ObjectInstance newObject =  new ObjectInstance(newGameObject);
				poolDictionary[poolKey].Enqueue (newObject);
				newObject.SetParent(poolHolder.transform);

				objects[i] = newGameObject;
			}
		}
		return objects;
	}

	public void ReuseObject(GameObject prefab, Vector3 position, Quaternion rotation) {
		int poolKey = prefab.GetInstanceID ();

		if(poolDictionary.ContainsKey (poolKey)) {
			ObjectInstance objectToReuse = poolDictionary [poolKey].Dequeue ();
			poolDictionary [poolKey].Enqueue (objectToReuse);

			objectToReuse.Reuse(position, rotation);
		}
	}

	public class ObjectInstance {

		GameObject gameObject;
		Transform transform;

		bool hasPoolObjectComponent;
		PoolObject poolObjectscript;

		public ObjectInstance(GameObject objectInstance) {
			gameObject = objectInstance;
			transform = gameObject.transform;
			gameObject.SetActive(false);

			if(gameObject.GetComponent<PoolObject>()) {
				hasPoolObjectComponent = true;
				poolObjectscript = gameObject.GetComponent<PoolObject>();
			}
		}

		public void Reuse(Vector3 position, Quaternion rotation) {
			if (hasPoolObjectComponent) {
				poolObjectscript.OnObjectReuse();
			}

			gameObject.SetActive (true);
			transform.position = position;
			transform.rotation = rotation;
		}

		public void SetParent(Transform parent) {
			transform.parent = parent;
		}
	}
}
