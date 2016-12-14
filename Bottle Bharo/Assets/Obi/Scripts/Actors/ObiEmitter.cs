using UnityEngine;
using System;
using System.Collections;


namespace Obi{

	[ExecuteInEditMode]
	[AddComponentMenu("Physics/Obi/Obi Emitter")]
	public class ObiEmitter : ObiActor {

		[SerializeField][HideInInspector] private ObiEmitterMaterial emitterMaterial = null;	
	
		[Tooltip("Amount of solver particles used by this emitter.")]
		[SerializeField][HideInInspector] private int numParticles = 1000;

		[Tooltip("Amount of particles emitted each second.")]
		public float emissionRate = 10;

		[Tooltip("Is the burst size automatically calculated from the emitter distribution?.")]
		public bool automaticBurstSize = true;

		[Tooltip("Amount of particles emitted simultaneously in a single frame.")]
		public int burstSize = 10;

		[Tooltip("Lifespan of each particle.")]
		public float lifespan = 4;

		[Tooltip("Initial speed of particles.")]
		public float initialSpeed = 0;

		[Range(0,1)]
		[Tooltip("Amount of randomization applied to particles.")]
		public float randomVelocity = 0;

		private ObiEmitterShape emitterShape = null;

		private int activeParticleCount = 0;			/**< number of currently active particles*/
		[HideInInspector] public float[] life;			/**< per particle remaining life in seconds.*/
		[HideInInspector] public float[] mass;			/**< per particle mass.*/

		private float unemittedTime = 0;

		public int NumParticles{
			set{
				if (numParticles != value){
					numParticles = value;
					GeneratePhysicRepresentation();
				}
			}
			get{return numParticles;}
		}

		public override bool SelfCollisions{
			get{return selfCollisions;}
		}

		public ObiEmitterMaterial EmitterMaterial{
			set{
				if (emitterMaterial != value){

					if (emitterMaterial != null)
					emitterMaterial.OnChangesMade -= EmitterMaterial_OnChangesMade;

					emitterMaterial = value;
				
					if (emitterMaterial != null){
						emitterMaterial.OnChangesMade += EmitterMaterial_OnChangesMade;
						EmitterMaterial_OnChangesMade(emitterMaterial,new ObiEmitterMaterial.MaterialChangeEventArgs(
																		  ObiEmitterMaterial.MaterialChanges.PER_MATERIAL_DATA |
																		  ObiEmitterMaterial.MaterialChanges.PER_PARTICLE_DATA)
													 );
					}
					
				}
			}
			get{
				return emitterMaterial;
			}
		}
	
		public override void Awake()
		{
			base.Awake();
			selfCollisions = true;
			GeneratePhysicRepresentation();
		}

		public override void OnEnable(){
			
			if (emitterMaterial != null)
				emitterMaterial.OnChangesMade += EmitterMaterial_OnChangesMade;			

			base.OnEnable();

		}
		
		public override void OnDisable(){

			if (emitterMaterial != null)
				emitterMaterial.OnChangesMade -= EmitterMaterial_OnChangesMade;	
			
			base.OnDisable();
			
		}

		public override void DestroyRequiredComponents(){
		}

		public override bool AddToSolver(object info){
			
			if (Initialized && base.AddToSolver(info)){

				// Get any emitter shape in this object:
				emitterShape = GetComponent<ObiEmitterShape>();

				// recalculate particle masses, as the number of dimensions used to valculate particle volume depends on the solver.
				SetParticleMassFromDensity();

				return true;
			}
			return false;
		}
		
		public override bool RemoveFromSolver(object info){
			return base.RemoveFromSolver(info);
		}


		/**
		 * Calculates particle mass using the fluid's rest density. The formula used for this is:
		 * mass = volume * density. Volume is approximated as that of a cube of side restRadius.
		 */
		private float CalculateParticleMass()
		{
			int dimensions = 3;
			if (solver != null && solver.parameters.mode == Oni.SolverParameters.Mode.Mode2D)
				dimensions = 2;

			// density = mass/(restDistance)^dimensions
			float restDistance = (emitterMaterial != null) ? emitterMaterial.restRadius : 0.1f ;
			float restDensity = (emitterMaterial != null) ? emitterMaterial.restDensity : 1000 ;
			return Mathf.Pow(restDistance,dimensions) * restDensity;
		}

		/**
		 * Sets all particle masses in accordance to the fluid's rest density.
		 */
		public void SetParticleMassFromDensity()
		{
			float pmass = CalculateParticleMass();

			for (int i = 0; i < invMasses.Length; i++){
				mass[i] = pmass;
				invMasses[i] = 1/mass[i];
			}

			this.PushDataToSolver(new ObiSolverData(ObiSolverData.ParticleData.INV_MASSES));
		}


		/**
		 * Sets particle solid radii to half of the fluids rest distance.
		 */
		public void SetParticleRestRadius(){
	
			// recalculate rest distance and particle mass:
			float restDistance = (emitterMaterial != null) ? emitterMaterial.restRadius : 0.1f ;

			for(int i = 0; i < particleIndices.Count; i++){
				solidRadii[i] = restDistance*0.5f;
			}

			PushDataToSolver(new ObiSolverData(ObiSolverData.ParticleData.SOLID_RADII));
		}

		/**
	 	* Generates the particle based physical representation of the emitter. This is the initialization method for the rope object
		* and should not be called directly once the object has been created.
	 	*/
		public void GeneratePhysicRepresentation()
		{		
			initialized = false;			
			initializing = true;

			RemoveFromSolver(null);

			active = new bool[numParticles];
			life = new float[numParticles];
			positions = new Vector3[numParticles];
			velocities = new Vector3[numParticles];
			vorticities = new Vector3[numParticles];
			invMasses  = new float[numParticles];
			solidRadii = new float[numParticles];
			phases = new int[numParticles];
			mass = new float[numParticles];

			float restDistance = (emitterMaterial != null) ? emitterMaterial.restRadius : 0.1f ;
			float pmass = CalculateParticleMass();
			
			for (int i = 0; i < numParticles; i++){

				active[i] = false;
				life[i] = 0;
				mass[i] = pmass;
				invMasses[i] = 1/mass[i];
				positions[i] = Vector3.zero;
				vorticities[i] = Vector3.zero;

				if (emitterMaterial != null && !emitterMaterial.isFluid)
					solidRadii[i] = restDistance*0.5f + UnityEngine.Random.Range(0,emitterMaterial.randomRadius);
				else
					solidRadii[i] = restDistance*0.5f;

				phases[i] = Oni.MakePhase(gameObject.layer,(selfCollisions?Oni.ParticlePhase.SelfCollide:0) |
														   ((emitterMaterial != null && emitterMaterial.isFluid)?Oni.ParticlePhase.Fluid:0));

			}
			
			AddToSolver(null);

			initializing = false;
			initialized = true;
			
		}

		public override void UpdateParticlePhases(){
	
			if (!InSolver) return;

			Oni.ParticlePhase fluidPhase = Oni.ParticlePhase.Fluid;
			if (emitterMaterial != null && !emitterMaterial.isFluid)
				fluidPhase = 0;
	
			for(int i = 0; i < particleIndices.Count; i++){
				phases[i] = Oni.MakePhase(gameObject.layer,(selfCollisions?Oni.ParticlePhase.SelfCollide:0) | fluidPhase);
			}
			PushDataToSolver(new ObiSolverData(ObiSolverData.ParticleData.PHASES));
		}

		void EmitterMaterial_OnChangesMade (object sender, ObiEmitterMaterial.MaterialChangeEventArgs e)
		{
			if ((e.changes & ObiEmitterMaterial.MaterialChanges.PER_PARTICLE_DATA) != 0){
				SetParticleMassFromDensity();
				SetParticleRestRadius();
				UpdateParticlePhases();
			}
		}

		public void ResetParticlePosition(int index){	

			if (emitterShape == null){

				Vector4[] posArray = {transform.position};
				Vector4[] velArray = {Vector3.Lerp(Vector3.zero,UnityEngine.Random.onUnitSphere,randomVelocity)};
				Oni.SetParticlePositions(solver.OniSolver,posArray,1,particleIndices[index]);
				Oni.SetParticleVelocities(solver.OniSolver,velArray,1,particleIndices[index]);
				Oni.SetParticleVorticities(solver.OniSolver,new Vector4[]{Vector4.zero},1,particleIndices[index]);

			}else{

				ObiEmitterShape.DistributionPoint distributionPoint = emitterShape.GetDistributionPoint();
				Vector3 spawnPosition = transform.TransformPoint(distributionPoint.position);

				Vector3 spawnVelocity = transform.TransformVector(distributionPoint.velocity);

				Vector4[] posArray = {spawnPosition};
				Vector4[] velArray = {Vector3.Lerp(spawnVelocity,UnityEngine.Random.onUnitSphere,randomVelocity) * initialSpeed};
				Oni.SetParticlePositions(solver.OniSolver,posArray,1,particleIndices[index]);
				Oni.SetParticleVelocities(solver.OniSolver,velArray,1,particleIndices[index]);
				Oni.SetParticleVorticities(solver.OniSolver,new Vector4[]{Vector4.zero},1,particleIndices[index]);

			}

		}

		/**
		 * Asks the emiter to emits a new particle. Returns whether the emission was succesful.
		 */
		public bool EmitParticle(){

			if (activeParticleCount == numParticles) return false;

			life[activeParticleCount] = lifespan;
			
			// move particle to its spawn position:
			ResetParticlePosition(activeParticleCount);

			// now there's one active particle more:
			active[activeParticleCount] = true;
			activeParticleCount++;

			return true;

		}

		/**
		 * Asks the emiter to kill a new particle. Returns whether the kill was succesful.
		 */
		public bool KillParticle(int index){

			if (activeParticleCount == 0 || index >= activeParticleCount) return false;

			// reduce amount of active particles:
			activeParticleCount--;
			active[activeParticleCount] = false; 

			// swap solver particle indices:
			int temp = particleIndices[activeParticleCount];
			particleIndices[activeParticleCount] = particleIndices[index];
			particleIndices[index] = temp;

			// also swap lifespans, so the swapped particle enjoys the rest of its life! :)
			float tempLife = life[activeParticleCount];
			life[activeParticleCount] = life[index];
			life[index] = tempLife;

			return true;
			
		}

		public override void OnSolverPreInterpolation(){

			base.OnSolverPreInterpolation();

			bool emitted = false;
			bool killed = false;

			// Update lifetime and kill dead particles:
			for (int i = activeParticleCount-1; i >= 0; --i){
				life[i] -= Time.deltaTime;

				if (life[i] <= 0){
					killed |= KillParticle(i);	
				}
			}

			// Calculate burst size:
			int effectiveBurstSize = 0;
			if (automaticBurstSize){
				effectiveBurstSize = (emitterShape != null)	? emitterShape.DistributionPointsCount : 1;
			}else{
				effectiveBurstSize = burstSize;
			}	

			// Emit new particles:
			unemittedTime += Time.deltaTime;
			while (unemittedTime > 0){
				for (int i = 0; i < effectiveBurstSize; ++i){
					emitted |= EmitParticle();
					unemittedTime -= 1 / emissionRate;
				}
			}

			// Push active array to solver if any particle has been killed or emitted this frame.
			if (emitted || killed){
				PushDataToSolver(new ObiSolverData(ObiSolverData.ParticleData.ACTIVE_STATUS));		
			}	

		}
	}
}
