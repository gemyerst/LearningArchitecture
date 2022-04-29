using System;

public class DeployRL
{
	public DeployRL()
	{

		//MAIN COPY

		//POLICY FROM TRAINING


		//ENVIRONMENT
		var env = Gym.Make('BoardGame');

		//MODEL
		var model = A2C(MlpPolicy, env, verbose = 1); //eg actor to critic
		model.Learn(totaltimesteps = 10000);

	}
}
