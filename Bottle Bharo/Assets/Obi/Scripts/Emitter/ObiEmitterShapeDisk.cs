using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;

namespace Obi
{

	[ExecuteInEditMode]
	public class ObiEmitterShapeDisk : ObiEmitterShape
	{
		
		public enum DiscSamplingMethod{
			GRID,
			REGULAR,
		}

		public DiscSamplingMethod samplingMethod = DiscSamplingMethod.REGULAR;
		public float radius = 0.5f;
		public float density = 32;	
		[Range(-1,1)]
		public float forwardVelocity = 1;
		[Range(-1,1)]
		public float outwardVelocity = 0;
		[Range(-1,1)]
		public float vortexVelocity = 0;
		public bool edgeEmission = false;

		private readonly float goldenAngle = Mathf.PI * (3 - Mathf.Sqrt(5));

		public void OnValidate(){
			radius = Mathf.Max(0,radius);
			density = Mathf.Max(0,density);
		}

		private Vector3 CalculateVelocity(Vector3 position){

			// calculate normalization factor:
			float total = Mathf.Abs(forwardVelocity) + Mathf.Abs(outwardVelocity) + Mathf.Abs(vortexVelocity);
			
			Vector3 outward = position;
			Vector3 vortex = new Vector3(-position.y,position.x);

			Vector3 velocity = (Vector3.forward * forwardVelocity/total + outward * outwardVelocity/total + vortex * vortexVelocity/total);
			return velocity/(velocity.magnitude + 0.00001f);

		}

		public override void GenerateDistribution(){

			distribution.Clear(); 

			if (edgeEmission)
			{
				float increment = 360.0f/density;
			
				for (float ang = 0; ang < 360; ang += increment)
				{
					Vector3 pos = new Vector3(Mathf.Cos(ang*Mathf.Deg2Rad)*radius,
											  Mathf.Sin(ang*Mathf.Deg2Rad)*radius,0);

					distribution.Add(new ObiEmitterShape.DistributionPoint(pos,CalculateVelocity(pos)));
				}
			}
			else
			{
	
				switch (samplingMethod)
				{
					case DiscSamplingMethod.GRID:
					{
		
						int num = Mathf.CeilToInt(Mathf.Sqrt(density)*0.5f);
						float norm = radius/(float)num;
	
						for (int x = -num; x < num; ++x){
							for (int y = -num; y < num; ++y){
			
								Vector3 pos = new Vector3(x,y,0) * norm;
			
								if (pos.magnitude < radius){
									distribution.Add(new ObiEmitterShape.DistributionPoint(pos,CalculateVelocity(pos)));
								}
			
							}
						}
		
					}break;
		
					case DiscSamplingMethod.REGULAR:
					{
						 
						// Vogel's method for spirals:
						int n = Mathf.FloorToInt(density);
	
						for (int i = 0; i < n; ++i)
						{
						    float theta = i * goldenAngle;
							float r = Mathf.Sqrt(i/(float)n) * radius;
	
							Vector3 pos = new Vector3(r * Mathf.Cos(theta), r * Mathf.Sin(theta),0);
	
							distribution.Add(new ObiEmitterShape.DistributionPoint(pos,CalculateVelocity(pos)));
						}
						
					}break;
				}
			}
		}


	#if UNITY_EDITOR
		public void OnDrawGizmosSelected(){

			Handles.matrix = transform.localToWorldMatrix;
			Handles.color  = Color.cyan;

			Handles.DrawWireDisc(Vector3.zero,Vector3.forward,radius);

			foreach (DistributionPoint point in distribution)
				Handles.ArrowCap(0,point.position,Quaternion.LookRotation(point.velocity),0.05f);

		}
	#endif

	}
}

