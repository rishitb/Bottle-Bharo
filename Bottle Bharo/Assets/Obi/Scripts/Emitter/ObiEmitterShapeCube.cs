using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;

namespace Obi
{

	[ExecuteInEditMode]
	public class ObiEmitterShapeCube : ObiEmitterShape
	{
		public Vector3 size = Vector3.one;
		public float density = 25;	

		public void OnValidate(){
			density = Mathf.Max(0,density);
		}

		public override void GenerateDistribution(){

			distribution.Clear(); 

			float numPerUnit = Mathf.Pow(density,1/3.0f);
			float spacing = 1/numPerUnit;

			int numX = Mathf.FloorToInt(size.x/spacing*0.5f);
			int numY = Mathf.FloorToInt(size.y/spacing*0.5f);
			int numZ = Mathf.FloorToInt(size.z/spacing*0.5f);

			for (int x = -numX; x <= numX; ++x){
				for (int y = -numY; y <= numY; ++y){
					for (int z = -numZ; z <= numZ; ++z){
						Vector3 pos = new Vector3(x,y,z)*spacing;
						Vector3 vel = Vector3.forward;

						distribution.Add(new ObiEmitterShape.DistributionPoint(pos,vel));

					}
				}
			}
	
		}


	#if UNITY_EDITOR
		public void OnDrawGizmosSelected(){

			Handles.matrix = transform.localToWorldMatrix;
			Handles.color  = Color.cyan;

			DrawWireCube(Vector2.zero,size);

			foreach (DistributionPoint point in distribution)
				Handles.ArrowCap(0,point.position,Quaternion.LookRotation(point.velocity),0.05f);

		}

		private void DrawWireCube(Vector3 position, Vector3 size){
           var half = size / 2;
           // draw front
           Handles.DrawLine(position + new Vector3(-half.x, -half.y, half.z), position + new Vector3(half.x, -half.y, half.z));
           Handles.DrawLine(position + new Vector3(-half.x, -half.y, half.z), position + new Vector3(-half.x, half.y, half.z));
           Handles.DrawLine(position + new Vector3(half.x, half.y, half.z), position + new Vector3(half.x, -half.y, half.z));
           Handles.DrawLine(position + new Vector3(half.x, half.y, half.z), position + new Vector3(-half.x, half.y, half.z));
           // draw back
           Handles.DrawLine(position + new Vector3(-half.x, -half.y, -half.z), position + new Vector3(half.x, -half.y, -half.z));
           Handles.DrawLine(position + new Vector3(-half.x, -half.y, -half.z), position + new Vector3(-half.x, half.y, -half.z));
           Handles.DrawLine(position + new Vector3(half.x, half.y, -half.z), position + new Vector3(half.x, -half.y, -half.z));
           Handles.DrawLine(position + new Vector3(half.x, half.y, -half.z), position + new Vector3(-half.x, half.y, -half.z));
           // draw corners
           Handles.DrawLine(position + new Vector3(-half.x, -half.y, -half.z), position + new Vector3(-half.x, -half.y, half.z));
           Handles.DrawLine(position + new Vector3(half.x, -half.y, -half.z), position + new Vector3(half.x, -half.y, half.z));
           Handles.DrawLine(position + new Vector3(-half.x, half.y, -half.z), position + new Vector3(-half.x, half.y, half.z));
           Handles.DrawLine(position + new Vector3(half.x, half.y, -half.z), position + new Vector3(half.x, half.y, half.z));
       }
	#endif

	}
}

