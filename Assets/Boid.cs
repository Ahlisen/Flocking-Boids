using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Boid : PoolObject {

	TrailRenderer trail;
	MeshRenderer meshRenderer;

	Vector3 velocity;
	Vector3 acceleration;

	float r;
	float maxforce;
	float maxspeed;

	float hue;

	List<Boid> inVicinity;
	float timeBetweenGroupCheck = 0.25f;
	float nextGroupCheck;
	float searchRadiusSqr = 6f;

	float screenHalfWidthWorld;
	float screenHalfHeightWorld;

	delegate Vector3 ForceDelegate(Boid other);

	void Awake() {
		trail = GetComponent<TrailRenderer>();
		meshRenderer = transform.GetChild(0).GetComponent<MeshRenderer>();
	}

	void Start () {
		screenHalfWidthWorld = Camera.main.aspect * Camera.main.orthographicSize;
		screenHalfHeightWorld = Camera.main.orthographicSize;

		velocity = new Vector3(0,0);
		acceleration = new Vector3(0,0);

		hue = Random.value;
		meshRenderer.material.color = Color.HSVToRGB(hue,0.5f,1f);

		r = 0.5f;
		maxspeed = 0.1f;
		maxforce = 0.005f;

		List<Boid> inVicinity = new List<Boid>();
		calculateVicinity();
	}
	
	// Update is called once per frame
	void Update () {

		wrap();
		
		Vector3 target = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		target.z = 0;

		Vector3 seekForce = seek(target);
		Vector3 separateForce = separate();
		Vector3 allignForce = normalizeNeighbors(other => other.velocity.normalized);//allign();
		Vector3 cohesionForce = normalizeNeighbors(other => other.transform.position.normalized); //cohesion();

		separateForce *= 1f;
		cohesionForce *= 0.2f;
		seekForce *= 0.2f;

		applyForce(separateForce);
		applyForce(seekForce);
		applyForce(allignForce);
		applyForce(cohesionForce);
				
		velocity += acceleration;
		velocity = Vector3.ClampMagnitude(velocity,maxspeed);
		transform.position += velocity;
		acceleration *= 0;
		
		float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

		if(Time.time > nextGroupCheck) {
			nextGroupCheck = Time.time + timeBetweenGroupCheck;
			calculateVicinity();
		}
		hue += getAverageColor();
		hue += (Random.value-.5f)*0.02f;
		hue = (hue + 1) % 1;
		meshRenderer.material.color = Color.HSVToRGB(Mathf.Clamp01(hue), 0.5f, 1);

	}

	public void calculateVicinity() {
		inVicinity = new List<Boid>();
		foreach(Boid other in Controller.instance.boids) {
			if(other == this) continue;

			Vector2 diff = transform.position - other.transform.position;
			float distance = Vector2.SqrMagnitude(diff);

			if(distance > 0 && distance < searchRadiusSqr) {
				inVicinity.Add(other);
			}
		}
	}

	public void applyForce(Vector3 force) {
		acceleration += force;
	}

	public Vector3 seek(Vector3 target) {
		Vector3 desired = target - transform.position;
		float d = Vector3.SqrMagnitude(desired);

		if(d < 10){
			desired.Normalize();
			float spd = d/10;
			spd = Mathf.Clamp(spd, 0, maxspeed);
			desired *= spd;
		} else {
			desired.Normalize();
			desired *= maxspeed;
		}

		Vector3 steer = desired - velocity;
		steer = Vector3.ClampMagnitude(steer, maxforce);
		
		return steer;
	}

	public Vector3 separate() {

		float desiredSeparation = r;
		Vector2 sum = new Vector2();
		int count = 0;

		foreach(Boid target in inVicinity) {

			Vector2 diff = transform.position - target.transform.position;
			float d = Vector2.SqrMagnitude(diff);

			if(d < desiredSeparation) {
				diff.Normalize();
				diff /= d;
				sum += diff;
				count++;
			}
		}

		if (count > 0) {
			sum /= count;

			Vector3 steer = new Vector3(sum.x,sum.y,0) - velocity;
			steer = Vector3.ClampMagnitude(steer, maxforce);

			return steer;
		}
		return Vector3.zero;
	}

	Vector3 normalizeNeighbors(ForceDelegate type) {
		Vector3 sum = new Vector3();
		int count = 0;

		foreach(Boid target in inVicinity) {
			sum += type(target);
			count++;
		}

		if (count > 0) {
			sum /= count;

			Vector3 steer = new Vector3(sum.x,sum.y,0) - velocity;
			steer = Vector3.ClampMagnitude(steer, maxforce);

			return steer;
		}
		return Vector3.zero;
	}

	public Vector3 allign() {
		Vector3 sum = new Vector3();
		int count = 0;

		foreach(Boid target in inVicinity) {
			sum += target.velocity.normalized;
			count++;
		}

		if (count > 0) {
			sum /= count;

			Vector3 steer = new Vector3(sum.x,sum.y,0) - velocity;
			steer = Vector3.ClampMagnitude(steer, maxforce);

			return steer;
		}
		return Vector3.zero;
	}

	public Vector3 cohesion() {
		Vector3 sum = new Vector3();
		int count = 0;

		foreach(Boid target in inVicinity) {
			sum += target.transform.position.normalized;
			count++;
		}

		if (count > 0) {
			sum /= count;

			Vector3 steer = new Vector3(sum.x,sum.y,0) - velocity;
			steer = Vector3.ClampMagnitude(steer, maxforce);

			return steer;
		}
		return Vector3.zero;
	}

	float getAverageColor () {
		float total = 0;
		float count = 0;
		foreach (Boid other in inVicinity) {
			
			if (other.hue - hue < -0.5f) {
				total += other.hue + 1 - hue;
			} else if (other.hue - hue > 0.5f) {
				total += other.hue - 1 - hue;
			} else {
				total += other.hue - hue; 
			}
			//total += other.hue;
			count++;
		}
		if (count == 0) return 0f;
		return total / count;
	}

	public override void OnObjectReuse () {
		velocity *= 0;
		trail.Clear();
	}

	void wrap () {

		if (transform.position.x > screenHalfWidthWorld) {
			transform.position = new Vector3 (-screenHalfWidthWorld, transform.position.y, 0);
		}
		
		if (transform.position.x < -screenHalfWidthWorld) {
			transform.position = new Vector3 (screenHalfWidthWorld, transform.position.y, 0);
		}

		if (transform.position.y > screenHalfHeightWorld) {
			transform.position = new Vector3 (transform.position.x, -screenHalfHeightWorld, 0);
		}

		if (transform.position.y < -screenHalfHeightWorld) {
			transform.position = new Vector3 (transform.position.x, screenHalfHeightWorld, 0);
		}
	}
}
