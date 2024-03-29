﻿using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
	 * Custom inspector for all ObiEmitterShape components. Just updates their point distribution when something changes. 
	 */

	[CustomEditor(typeof(ObiEmitterShape), true), CanEditMultipleObjects] 
	public class ObiEmitterShapeEditor : Editor
	{
	
		ObiEmitterShape shape;
		
		public void OnEnable(){
			shape = (ObiEmitterShape)target;
		}
		
		public override void OnInspectorGUI() {
			
			serializedObject.UpdateIfDirtyOrScript();
			
			Editor.DrawPropertiesExcluding(serializedObject,"m_Script");
			
			// Apply changes to the serializedProperty
			if (GUI.changed){
				serializedObject.ApplyModifiedProperties();
				shape.GenerateDistribution();
			}
			
		}
		
	}

}

