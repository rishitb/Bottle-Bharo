using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;

namespace Obi
{

	[ExecuteInEditMode]
	public class ObiEmitterShapeSphere : ObiEmitterShape
	{
		
		public enum SphereSamplingMethod{
			GRID,
			REGULAR_SURFACE,
		}

		public SphereSamplingMethod samplingMethod = SphereSamplingMethod.REGULAR_SURFACE;
		public float radius = 0.5f;
		public float density = 10;	

		private readonly float goldenAngle = Mathf.PI * (3 - Mathf.Sqrt(5));

		public void OnValidate(){
			radius = Mathf.Max(0.001f,radius);
			density = Mathf.Max(0,density);
		}

		public override void GenerateDistribution(){

			distribution.Clear(); 

			switch (samplingMethod)
			{
				case SphereSamplingMethod.GRID:
				{
	
					int num = Mathf.CeilToInt(Mathf.Pow(density,1/3.0f)*0.5f);
					float norm = radius/(float)num;

					for (int x = -num; x <= num; ++x){
						for (int y = -num; y <= num; ++y){
							for (int z = -num; z <= num; ++z){
								Vector3 pos = new Vector3(x,y,z) * norm;
								Vector3 vel = pos.normalized;
			
								if (pos.magnitude < radius){
									distribution.Add(new ObiEmitterShape.DistributionPoint(pos,vel));
								}
							}
						}
					}
	
				}break;

				case SphereSamplingMethod.REGULAR_SURFACE:
				{
					 
					// Vogel's method for spirals:
					int n = Mathf.FloorToInt(density);
					float offset = 2f/n;

					for (int i = 0; i < n; ++i)
					{
					    float theta = i * goldenAngle;
						float z = i*offset-1  + offset*0.5f;
						float r = Mathf.Sqrt(1 - z*z);

						Vector3 pos = new Vector3(r*Mathf.Cos(theta),r*Mathf.Sin(theta),z ) * radius;
						Vector3 vel = pos.normalized;

						distribution.Add(new ObiEmitterShape.DistributionPoint(pos,vel));
					}
					
				}break;
			}
		}


	#if UNITY_EDITOR
		public void OnDrawGizmosSelected(){

			Handles.matrix = transform.localToWorldMatrix;
			Handles.color  = Color.cyan;

			Handles.DrawWireDisc(Vector3.zero,Vector3.forward,radius);
			Handles.DrawWireDisc(Vector3.zero,Vector3.up,radius);
			Handles.DrawWireDisc(Vector3.zero,Vector3.right,radius);

			foreach (DistributionPoint point in distribution)
				Handles.ArrowCap(0,point.position,Quaternion.LookRotation(point.velocity),0.05f);

		}
	#endif

	}
}

