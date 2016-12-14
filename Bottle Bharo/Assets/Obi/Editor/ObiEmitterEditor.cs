using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
	 * Custom inspector for ObiEmitter components.
	 * Allows particle emission and constraint edition. 
	 * 
	 * Selection:
	 * 
	 * - To select a particle, left-click on it. 
	 * - You can select multiple particles by holding shift while clicking.
	 * - To deselect all particles, click anywhere on the object except a particle.
	 * 
	 * Constraints:
	 * 
	 * - To edit particle constraints, select the particles you wish to edit.
	 * - Constraints affecting any of the selected particles will appear in the inspector.
	 * - To add a new pin constraint to the selected particle(s), click on "Add Pin Constraint".
	 * 
	 */
	[CustomEditor(typeof(ObiEmitter)), CanEditMultipleObjects] 
	public class ObiEmitterEditor : ObiParticleActorEditor
	{
		
		[MenuItem("Assets/Create/Obi/Obi Emitter Material")]
		public static void CreateObiEmitterMaterial ()
		{
			ObiEditorUtils.CreateAsset<ObiEmitterMaterial> ();
		}

		[MenuItem("Component/Physics/Obi/Obi Emitter",false,0)]
		static void AddObiRope()
		{
			foreach(Transform t in Selection.transforms)
				Undo.AddComponent<ObiEmitter>(t.gameObject);
		}

		[MenuItem("GameObject/3D Object/Obi/Obi Emitter",false,4)]
		static void CreateObiCloth()
		{
			GameObject c = new GameObject("Obi Emitter");
			Undo.RegisterCreatedObjectUndo(c,"Create Obi Emitter");
			ObiEmitter em = c.AddComponent<ObiEmitter>();
			c.AddComponent<ObiEmitterShapeDisk>();

			GameObject p = new GameObject("Obi Particle Renderer");
			Undo.RegisterCreatedObjectUndo(p,"Create Obi Particle Renderer");
			ObiParticleRenderer pr = p.AddComponent<ObiParticleRenderer>();

			p.transform.parent = c.transform;

			pr.Actor = em;
		}

		[MenuItem("GameObject/3D Object/Obi/Obi Emitter (with solver)",false,5)]
		static void CreateObiClothWithSolver()
		{

			GameObject c = new GameObject("Obi Emitter");
			Undo.RegisterCreatedObjectUndo(c,"Create Obi Emitter");
			ObiEmitter em = c.AddComponent<ObiEmitter>();
			c.AddComponent<ObiEmitterShapeDisk>();

			GameObject p = new GameObject("Obi Particle Renderer");
			Undo.RegisterCreatedObjectUndo(p,"Create Obi Particle Renderer");
			ObiParticleRenderer pr = p.AddComponent<ObiParticleRenderer>();

			p.transform.parent = c.transform;

			pr.Actor = em;

			ObiSolver solver = c.AddComponent<ObiSolver>();
			ObiColliderGroup group = c.AddComponent<ObiColliderGroup>();
			em.Solver = solver;
			solver.colliderGroup = group;

		}
		
		ObiEmitter emitter;
		
		public override void OnEnable(){
			base.OnEnable();
			emitter = (ObiEmitter)target;
		}
		
		public override void OnDisable(){
			base.OnDisable();
			EditorUtility.ClearProgressBar();
		}

		public override void UpdateParticleEditorInformation(){
			
			for(int i = 0; i < emitter.positions.Length; i++)
			{
				wsPositions[i] = emitter.GetParticlePosition(i);		
			}

			//if (rope.clothMesh != null){
				for(int i = 0; i < emitter.positions.Length; i++)
				{
					facingCamera[i] = IsParticleFacingCamera(Camera.current, i);
				}
			//}
			
		}

		public bool IsParticleFacingCamera(Camera cam, int particleIndex){

			return true;

		}
		
		protected override void SetPropertyValue(ParticleProperty property,int index, float value){
			
			switch(property){
			case ParticleProperty.MASS: 
				emitter.mass[index] = value;
				float areaMass = emitter.mass[index]; //* rope.areaContribution[index];
				if (areaMass > 0){
					emitter.invMasses[index] = 1 / areaMass;
				}else{
					emitter.invMasses[index] = 0;
				}
				break; 
			}
			
		}
		
		protected override float GetPropertyValue(ParticleProperty property, int index){
			switch(property){
				case ParticleProperty.MASS:{
					return emitter.mass[index];
				}
			}
			return 0;
		}

		public override void OnInspectorGUI() {
			
			serializedObject.Update();

			emitter.Solver = EditorGUILayout.ObjectField("Solver",emitter.Solver, typeof(ObiSolver), true) as ObiSolver;

			emitter.EmitterMaterial = EditorGUILayout.ObjectField(new GUIContent("Emitter material","Emitter material used. This controls the behavior of the emitted particles."),
																  emitter.EmitterMaterial, typeof(ObiEmitterMaterial), false) as ObiEmitterMaterial;

			emitter.NumParticles = EditorGUILayout.IntField(new GUIContent("Num particles","Amount of pooled particles used by this emitter."), emitter.NumParticles);

			Editor.DrawPropertiesExcluding(serializedObject,"m_Script");
			
			// Apply changes to the serializedProperty
			if (GUI.changed){
				emitter.UpdateParticlePhases(); //TODO: only do this when changing material.
				serializedObject.ApplyModifiedProperties();
			}
			
		}
		
	}
}




