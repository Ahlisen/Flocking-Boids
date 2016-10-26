using UnityEngine;
using System.Collections;

public class Controller : MonoBehaviour {

	public GameObject prefab;
	public Boid[] boids;
	public int amountOfBoids = 50;

	public static Controller instance;

	void Awake() {
		instance = this;
	}

	void Start () {
		GameObject[] objects = PoolManager.instance.CreatePool (prefab, amountOfBoids);
		boids = new Boid[objects.Length];

		for(int i = 0; i < objects.Length; ++i) {
			boids[i] = objects[i].GetComponent<Boid>();
		}

		PoolManager.instance.ReuseObject(prefab, Vector3.zero, Quaternion.identity);
	}
	
	void Update () {
		if (Input.GetKey (KeyCode.Mouse0)) {
			Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			pos += new Vector3(Random.value*5-2.5f,Random.value*5-2.5f,0);
			pos.z = 0;
			PoolManager.instance.ReuseObject(prefab, pos, Quaternion.identity);
		}/*
		foreach(Boid boid in boids) {
			Vector3 tar = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			tar.z = 0;

			if (boid.isActiveAndEnabled) {
				Vector3 separateForce = boid.separate(boids);
				Vector3 seekForce = boid.seek(tar);

				separateForce *= 1.5f;

				boid.applyForce(separateForce);
				boid.applyForce(seekForce);
			}
		}
		*/
	}
}
