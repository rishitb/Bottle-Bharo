using UnityEngine;
using System.Collections;
using Obi;

[RequireComponent(typeof(ObiSolver))]
public class ObiFluidEventHandler : MonoBehaviour {

 	ObiSolver solver;

	void Awake(){
		solver = GetComponent<Obi.ObiSolver>();
	}

	void OnEnable () {
		solver.OnFluidUpdated += Solver_OnFluidUpdated;
	}

	void OnDisable(){
		solver.OnFluidUpdated -= Solver_OnFluidUpdated;
	}
	
	void Solver_OnFluidUpdated (object sender, Obi.ObiSolver.ObiFluidEventArgs e)
	{
		for(int i = 0;  i < e.indices.Length; ++i){
			//Debug.DrawRay(solver.renderablePositions[e.indices[i]],e.vorticities[e.indices[i]]*0.05f,Color.red);
			Debug.DrawRay(solver.renderablePositions[e.indices[i]],-e.normals[e.indices[i]]*0.5f,Color.blue);
		}
	}

}
