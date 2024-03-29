﻿using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;

namespace Obi
{

	[ExecuteInEditMode]
	public class ObiEmitterShapeEdge : ObiEmitterShape
	{

		public float lenght = 0.5f;
		public float amount = 10;	
		public float radialVelocity = 0;

		public void OnValidate(){
			lenght = Mathf.Max(0,lenght);
			amount = Mathf.Max(0,amount);
		}

		public override void GenerateDistribution(){

			distribution.Clear(); 
		
			float increment = lenght/Mathf.Max(1,amount-1);
		
			for (int i = 0; i < amount; ++i)
			{
				Vector3 pos = new Vector3(i*increment - lenght*0.5f,0,0);
				Vector3 vel = Quaternion.AngleAxis(i*radialVelocity,Vector3.right) * Vector3.forward;
				distribution.Add(new ObiEmitterShape.DistributionPoint(pos,vel));
			}

		}

	#if UNITY_EDITOR
		public void OnDrawGizmosSelected(){

			Handles.matrix = transform.localToWorldMatrix;
			Handles.color  = Color.cyan;

			Handles.DrawLine(-Vector3.right*lenght*0.5f,Vector3.right*lenght*0.5f);

			foreach (DistributionPoint point in distribution)
				Handles.ArrowCap(0,point.position,Quaternion.LookRotation(point.velocity),0.05f);
		}
	#endif

	}
}

