using UnityEngine;
using System;
using System.Collections.Generic;


namespace Obi{

	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	public abstract class ObiEmitterShape : MonoBehaviour
	{

		[Serializable]
		public struct DistributionPoint{
			public Vector3 position;
			public Vector3 velocity;

			public DistributionPoint(Vector3 position, Vector3 velocity){
				this.position = position;
				this.velocity = velocity;
			}
		}

		protected List<DistributionPoint> distribution = new List<DistributionPoint>();
		protected int lastDistributionPoint = 0;

		public int DistributionPointsCount{
			get{return distribution.Count;}
		}

		public void Awake(){
			GenerateDistribution();
		}

		public abstract void GenerateDistribution();

		public DistributionPoint GetDistributionPoint(){

			if (lastDistributionPoint >= distribution.Count)
				return new DistributionPoint();

			DistributionPoint point = distribution[lastDistributionPoint];
			lastDistributionPoint = (lastDistributionPoint + 1) % distribution.Count;

			return point;
			
		}
		
	}
}

