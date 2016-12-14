using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

namespace Obi{

[ExecuteInEditMode]
[RequireComponent(typeof(ParticleSystem))]
public class ParticleAdvector : MonoBehaviour {

	public ObiSolver solver;

	private ParticleSystem particleSystem;
	private ParticleSystem.Particle[] particles;

	Vector4[] positions;
	Vector4[] velocities;
	int[] active;
		//int[] neighbourCounts;

	int alive;

	void OnEnable(){
		if (solver != null){
			solver.OnStepBegin += Solver_OnStepBegin;
			solver.OnStepEnd += Solver_OnStepEnd;
		}
	}

	void OnDisable(){
		if (solver != null){
			solver.OnStepBegin -= Solver_OnStepBegin;
			solver.OnStepEnd -= Solver_OnStepEnd;
		}
	}

	void Initialize()
	{
		if (particleSystem == null)
			particleSystem = GetComponent<ParticleSystem>();

		if (particles == null || particles.Length < particleSystem.maxParticles)
			particles = new ParticleSystem.Particle[particleSystem.maxParticles]; 
	}

	void Solver_OnStepBegin (object sender, System.EventArgs e)
	{
		if (solver == null) return;

		Initialize();

		alive = particleSystem.GetParticles(particles);

		positions = new Vector4[alive];
		velocities = new Vector4[alive];
		active = new int[alive];
		//neighbourCounts = new int[alive];
		for (int i = 0; i < alive; ++i){
			positions[i] = particles[i].position;
			velocities[i] = particles[i].velocity;
			active[i] = i;
		}

		//neighboursHandle = Oni.PinMemory(neighbourCounts);

		Oni.SetActiveDiffuseParticles(solver.OniSolver,active,alive);
		Oni.SetDiffuseParticlePositions(solver.OniSolver,positions,alive,0);
		Oni.SetDiffuseParticleVelocities(solver.OniSolver,velocities,alive,0);
		//Oni.SetDiffuseParticleNeighbourCounts(solver.Solver,neighboursHandle.AddrOfPinnedObject());
	}

	void Solver_OnStepEnd (object sender, System.EventArgs e)
	{

		if (solver == null) return;

		Vector4 antiGravity = -Physics.gravity * particleSystem.gravityModifier * Time.fixedDeltaTime;

		Oni.GetDiffuseParticleVelocities(solver.OniSolver,velocities,alive,0);

		for (int i = 0; i < alive; ++i){

			//if (neighbourCounts[i] >= 2){
				particles[i].velocity = velocities[i] + antiGravity;
			/*}else{
				particles[i].lifetime -= 2;
			}*/
		}

		particleSystem.SetParticles(particles, alive);
		particleSystem.Simulate(Time.fixedDeltaTime,false,false);

	}
}
}