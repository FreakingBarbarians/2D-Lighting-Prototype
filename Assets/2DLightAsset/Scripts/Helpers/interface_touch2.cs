using UnityEngine;
using System.Collections;

public class interface_touch2: MonoBehaviour {
	
	GameObject cLight;
	GameObject cubeL;
	Camera cam;
	
	//GUIText UIlights;
	//GUIText UIvertex;


	[HideInInspector] public static int vertexCount;

	int lightCount = 1;


	bool _mouseClick;
	bool _ctrlDown;


	void Start () {

		cam = GameObject.Find("Camera").GetComponent<Camera>();

		StartCoroutine(LoopUpdate());
	}

	void Update()
	{
		_mouseClick = Input.GetMouseButtonDown (0);
		_ctrlDown = Input.GetKey(KeyCode.LeftControl);
	}
	
	// Update is called once per frame
	IEnumerator LoopUpdate () {

		while (true){
			cLight = GameObject.Find("2DLight");
			//if(Input.GetAxis("Horizontal")){
			//light.transform.position = new Vector3 (Input.mousePosition.x -Screen.width*.5f, Input.mousePosition.y -Screen.height*.5f);
			Vector3 pos = cLight.transform.position;
			pos.x += Input.GetAxis ("Horizontal") * 30f * Time.deltaTime;
			pos.y += Input.GetAxis ("Vertical") * 30f * Time.deltaTime;

			
			
			if (_mouseClick) {
				
				Vector2 p = cam.ScreenToWorldPoint(Input.mousePosition);
				
				if(_ctrlDown == true){
					Material m = new Material( cLight.GetComponent<DynamicLight>().lightMaterial as Material); 
					
					
					GameObject nLight = new GameObject();
					nLight.transform.parent = cLight.transform;
					
					nLight.AddComponent<DynamicLight>();
					//m.color = new Color(Random.Range(0f,1f),Random.Range(0f,1f),Random.Range(0f,1f));
					nLight.GetComponent<DynamicLight>().lightMaterial = m;
					nLight.transform.position = p;
					nLight.GetComponent<DynamicLight>().lightRadius = 40;
					
					GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
					quad.transform.parent = nLight.transform;
					quad.transform.localPosition = Vector3.zero;
					lightCount++;
					
				}
				
				
				
			}


			yield return new WaitForEndOfFrame();
			cLight.transform.position = pos;



		}
	
	}



}
