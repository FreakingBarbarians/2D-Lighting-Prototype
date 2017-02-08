using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class LightVertex : System.IEquatable<LightVertex>, IComparable<LightVertex> {
    public Vector3 pos;
    public float intensity;
    public float angle;
    public int extendedPoint; // -1 left, 0 mid, 1 right
    public int incident;

    public LightVertex(Vector3 pos, float intensity, float angle, int extendedPoint) {
        this.pos = pos;
        this.intensity = intensity; // Also the color in grayscale. 1 white, 0 black. Clamp between 0,1
        this.angle = angle;
        this.extendedPoint = extendedPoint;
    }

    public bool Equals(LightVertex other) {
        if (other == null) {
            return false;
        }
        else if (other.angle == this.angle) {
            return true;
        }
        else {
            return false;
        }
    }

    public int CompareTo(LightVertex other) {
        if (other == null) {
            return -1;
        }
        else if (other.angle > this.angle) {
            return -1;
        }
        else if (other.angle < this.angle) {
            return 1;
        }
        else {
            if (this.incident == 0 && other.incident == 0) {
                // Debug.Log("WeirdIncident");
                Debug.DrawLine(Camera.main.transform.position + this.pos, Camera.main.transform.position + other.pos, Color.red);
            }
            if (this.incident == 1) {
                return -1;
            }
            if (this.incident == -1) {
                return 1;
            }
            return 0;
        }
    }
}

public class MyPointLight : MonoBehaviour {

    private static readonly float PI = 3.141592653f;

    List<LightVertex> vertices = new List<LightVertex>();

    Mesh lightMesh;
    public float range;
    public float intensity;
    public int outerRoundness;
    public Material lightMaterial;
    private MeshRenderer rendy;
    private MeshFilter filter;

    // Use this for initialization
    void Start() {
        lightMesh = new Mesh();
        lightMesh.MarkDynamic();

        filter = gameObject.GetComponent<MeshFilter>();
        filter.mesh = lightMesh;

        rendy = gameObject.GetComponent<MeshRenderer>();
    }

    private void generateLightMesh() {

        // Clear our meshes and lists
        vertices.Clear();
        lightMesh.Clear();

        // first vertex always first
        vertices.Add(new LightVertex(new Vector3(0, 0, 0), intensity, -1, -1));

        // List<float[]> occludedAngles = new List<float[]>();

        // Cast onto all colliders within a radius. transfer them into an array.
        RaycastHit2D[] hits = Physics2D.CircleCastAll(gameObject.transform.position, range, Vector2.zero);
        PolygonCollider2D[] colliders = new PolygonCollider2D[hits.Length];

        for (int i = 0; i < hits.Length; i++) {
            colliders[i] = (PolygonCollider2D)hits[i].collider;
        }


        foreach (PolygonCollider2D coll in colliders) {

            // For each polygon collider, find the points facing the light
            List<Vector2> pointsFacingLight = new List<Vector2>();

            foreach (Vector2 vertex in coll.points) {
                Vector2 deltaPos = coll.transform.position + (Vector3)vertex - gameObject.transform.position;
                if (occlusionChecking(deltaPos)) {
                    pointsFacingLight.Add(deltaPos);
                }
            }

            foreach (Vector2 deltaPos in pointsFacingLight) {

                float angle = get360Angle(deltaPos);
                // Find end points later int endpoint = endChecking(deltaPos);

                // draw the point
                Debug.DrawLine((Vector2)coll.transform.position + deltaPos, (Vector2)coll.transform.position + deltaPos + Vector2.up, Color.blue);

                vertices.Add(new LightVertex(deltaPos, intensity / deltaPos.magnitude, angle, 0));

                //Color debug;
                //if (endpoint == -1) {
                //    debug = Color.blue;
                //}
                //else if (endpoint == 1) {
                //    debug = Color.red;
                //}
                //else {
                //    debug = Color.white;
                //}

                // Debug.DrawLine(gameObject.transform.position, gameObject.transform.position + (Vector3) deltaPos, debug);

                // Create Pass Through Position if EndPoint
                //if (endpoint == 1) {
                //    RaycastHit2D[] passThrough = Physics2D.RaycastAll(gameObject.transform.position, deltaPos, range);
                //    //  occludedAngles.Add(new float[2] { angle, 1 });
                //    if (passThrough.Length < 2) {
                //        Vector2 passThroughPos = deltaPos.normalized * range;
                //        vertices.Add(new LightVertex(passThroughPos, intensity / passThroughPos.magnitude, angle, 2));
                //    }
                //    else {
                //        Vector2 passThroughPos = passThrough[1].point - (Vector2) transform.position;
                //        vertices.Add(new LightVertex(passThroughPos, intensity / passThroughPos.magnitude, angle, 2));
                //    }
                //}
                //else if (endpoint == -1) {
                //    RaycastHit2D[] passThrough = Physics2D.RaycastAll(gameObject.transform.position, deltaPos, range);
                //    // occludedAngles.Add(new float[2] { angle, -1 });
                //    if (passThrough.Length < 2) {
                //        Vector2 passThroughPos = deltaPos.normalized * range;
                //        vertices.Add(new LightVertex(passThroughPos, intensity / passThroughPos.magnitude, angle, -2));
                //    }
                //    else {
                //        Vector2 passThroughPos = passThrough[1].point - (Vector2)transform.position;
                //        vertices.Add(new LightVertex(passThroughPos, intensity / passThroughPos.magnitude, angle, -2));
                //    }
                //}

                // Add collider vertex
                //vertices.Add(new LightVertex(deltaPos, intensity / deltaPos.magnitude, angle, endpoint));

            }

            // find endpoints

            // Vector to the mid of the collider from mid of the light 
            Vector2 midDeltaPos = coll.transform.position - gameObject.transform.position;
            // Left most point, is our left endpoint
            Vector2 leftMost = midDeltaPos; // set to the center of the collider as default
            float leftMostValue = -1;
            // Right most point, is our right endpoint
            Vector2 rightMost = midDeltaPos;
            float rightMostValue = -1;
            // Left perpendicular from mid
            Vector2 leftPerpendicular = new Vector2(-midDeltaPos.y, midDeltaPos.x);
            // Right perpendicular from mid
            Vector2 rightPerpendicular = new Vector2(midDeltaPos.y, -midDeltaPos.x);

            // Check if each endpoint is occluded or not.
            foreach (Vector2 vertex in coll.points) {
                Vector2 deltapos = coll.transform.position + (Vector3)vertex - gameObject.transform.position;
                // a dot b is the projection of a onto b
                float leftScalar = Vector2.Dot(deltapos, leftPerpendicular);
                float rightScalar = Vector2.Dot(deltapos, rightPerpendicular);

                if (leftScalar > leftMostValue) {
                    leftMost = deltapos;
                    leftMostValue = leftScalar;
                }
                else if (rightScalar > rightMostValue) {
                    rightMost = deltapos;
                    rightMostValue = rightScalar;
                }
            }

            Debug.DrawRay((Vector2)coll.transform.position + leftMost, Vector2.up, Color.red);
            Debug.DrawRay((Vector2)coll.transform.position + rightMost, Vector2.up, Color.green);

            // check if occluded
            if (occlusionChecking(leftMost)) {
                    float angle = get360Angle(leftMost);
                    RaycastHit2D[] passThrough = Physics2D.RaycastAll(gameObject.transform.position, leftMost, range);
                    //  occludedAngles.Add(new float[2] { angle, 1 });
                    if (passThrough.Length < 2) {
                        Vector2 passThroughPos = leftMost.normalized * range;

                        vertices.Add(new LightVertex(passThroughPos, intensity / passThroughPos.magnitude, angle, -1));
                    }
                    else {
                        Vector2 passThroughPos = passThrough[1].point - (Vector2)transform.position;
                        vertices.Add(new LightVertex(passThroughPos, intensity / passThroughPos.magnitude, angle, -1));
                    }
                }
            if (occlusionChecking(rightMost)) {
                float angle = get360Angle(rightMost);
                RaycastHit2D[] passThrough = Physics2D.RaycastAll(gameObject.transform.position, rightMost, range);
                //  occludedAngles.Add(new float[2] { angle, 1 });
                if (passThrough.Length < 2) {
                    Vector2 passThroughPos = rightMost.normalized * range;
                    vertices.Add(new LightVertex(passThroughPos, intensity / passThroughPos.magnitude, angle, 1));
                }
                else {
                    Vector2 passThroughPos = passThrough[1].point - (Vector2)transform.position;
                    vertices.Add(new LightVertex(passThroughPos, intensity / passThroughPos.magnitude, angle, 1));
                }
            }
        }

        // create default vertices, not including occluded.

        //occludedAngles = cleanAngles(occludedAngles);

        float degPerVertex = 360 / outerRoundness;
        float radPerVertex = 2 * PI / outerRoundness;
        for (int i = 0; i < outerRoundness; i++) {
            RaycastHit2D hit = Physics2D.Raycast(gameObject.transform.position, new Vector2(Mathf.Cos(radPerVertex * i), Mathf.Sin(radPerVertex * i)), range);
            if (!hit) {
                Vector3 position = new Vector3(Mathf.Cos(radPerVertex * i), Mathf.Sin(radPerVertex * i), 0) * range;
                position.z = gameObject.transform.position.z;
                LightVertex vert = new LightVertex(position, intensity / range, degPerVertex * i, 0);
                vertices.Add(vert);
            }
        }

        // sort array
        vertices.Sort();
        List<int> triangles = new List<int>();
        foreach (LightVertex vertex in vertices) {
            for (int i = 1; i < vertices.Count - 1; i++) {
                triangles.Add(0);
                triangles.Add(i + 1);
                triangles.Add(i);
            }
            if (vertices.Count > 1) {
                triangles.Add(0);
                triangles.Add(1);
                triangles.Add(vertices.Count - 1);
            }
        }

        Vector3[] verticesArray = new Vector3[vertices.Count];
        for (int i = 0; i < vertices.Count; i++) {
            verticesArray[i] = vertices[i].pos;
        }

        lightMesh.vertices = verticesArray;
        lightMesh.triangles = triangles.ToArray();

    }


    // Update is called once per frame
    void Update() {
        generateLightMesh();
    }

    private float get360Angle(Vector2 vector) {
        if (vector.y >= 0) {
            return (Vector2.Angle(Vector2.right, vector));
        }
        else {
            return 360 - +Vector2.Angle(Vector2.right, vector);
        }
    }

    private bool occlusionChecking(Vector3 deltaPos) {

        // if further than range occluded by fog
        if (deltaPos.magnitude > range) {
            return false;
        }

        RaycastHit2D hit = Physics2D.Raycast(gameObject.transform.position, deltaPos, deltaPos.magnitude - 0.1f);
        if (!hit) {
            return true;
        }
        else {
            return false;
        }
    }

    private int endChecking(Vector2 deltaPos) {
        RaycastHit2D hit;
        Vector2 leftHandPerp = new Vector2(-deltaPos.y, deltaPos.x);
        Vector2 rightHandPerp = new Vector2(deltaPos.y, -deltaPos.x);
        leftHandPerp.Normalize();
        rightHandPerp.Normalize();

        Vector2 lhCheck = deltaPos + leftHandPerp * 0.001f;
        Vector2 rhCheck = deltaPos + rightHandPerp * 0.001f;

        // Check left side
        hit = Physics2D.Raycast(gameObject.transform.position, lhCheck, range);
        if (!hit) {
            return -1;
        }
        else {
            // check right side
            hit = Physics2D.Raycast(gameObject.transform.position, rhCheck, range);
            if (!hit) {
                return 1;
            }
            else {
                // its midpoint
                return 0;
            }
        }
    }
}

