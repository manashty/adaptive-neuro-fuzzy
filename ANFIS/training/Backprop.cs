﻿using NeuroFuzzy.misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroFuzzy.training
{
    public class Backprop : ITraining
    {
        double learningRate = 1e-10;
        double lastError = double.MaxValue;
        double abstol, reltol, adjustThreshold;
        bool isStop = false;

        public event UnknownCase UnknownCaseFaced;

        public Backprop(double LearningRate, double abstol = 1e-4, double reltol = 1e-7, double adjustThreshold=1e-15)
        {
            this.learningRate = LearningRate;
            this.abstol = abstol;
            this.reltol = reltol;
            this.adjustThreshold = adjustThreshold;
        }

        public double Iteration(double[][] x, double[][] y, IList<IRule> ruleBase)
        {
Restart:
            isStop = false;
            
            if (x.Length != y.Length)
                throw new Exception("Input and desired output lengths not match");
            if (ruleBase == null || ruleBase.Count == 0)
                throw new Exception("Incorrect rulebase");
            
            int outputDim = ruleBase[0].Z.Length;
            int numOfRules = ruleBase.Count;

            double globalError = 0.0;

            double[] firings = new double[numOfRules];

            for (int sample = 0; sample < x.Length; sample++)
            {
                double[] o = new double[outputDim];
                double firingSum = 0.0;

                for (int i = 0; i < numOfRules; i++)
                {
                    firings[i] = ruleBase[i].Membership(x[sample]);
                    firingSum += firings[i];
                }

                if (UnknownCaseFaced != null && firingSum < adjustThreshold)
                {
                    int neig = math.NearestNeighbourhood(ruleBase.Select(z => z.Centroid).ToArray(), x[sample]);
                    UnknownCaseFaced(ruleBase, x[sample], y[sample], ruleBase[neig].Centroid);
                    Console.WriteLine("Adjusting rule base. Now {0} are in base.", ruleBase.Count);
                    goto Restart;
                }

                for (int i = 0; i < numOfRules; i++)
                    for (int C = 0; C < outputDim; C++)
                        o[C] += firings[i] / firingSum * ruleBase[i].Z[C];

                for (int rule = 0; rule < ruleBase.Count; rule++)
                {
                    double[] parm = ruleBase[rule].Parameters;
                    double[] grad = ruleBase[rule].GetGradient(x[sample]);

                    for (int p = 0; p < parm.Length; p++)
                    {
                        double g = dEdP(y[sample], o, ruleBase, firings, grad, firingSum, rule, outputDim, numOfRules, p);

                        parm[p] -= learningRate * g;
                    }
                }

                for (int i = 0; i < numOfRules; i++)
                    for (int C = 0; C < outputDim; C++)
                    {
                        ruleBase[i].Z[C] -= learningRate * (o[C] - y[sample][C]) * firings[i] / firingSum;
                    }

                for (int C = 0; C < outputDim; C++)
                    globalError += Math.Abs(o[C] - y[sample][C]);
            }

            checkStop(globalError);

            return globalError / x.Length;
        }

      

        private void checkStop(double globalError)
        {
            if (globalError < abstol)
                isStop = true;

            if (Math.Abs(lastError - globalError) < reltol)
                isStop = true;

            lastError = globalError;
        }

        private static double dEdP(double[] y, double[] o,
            IList<IRule> z, 
            double[] firings, 
            double[] grad, 
            double firingSum, 
            int rule, 
            int outputDim, 
            int numOfRules, 
            int p)
        {
            double g = 0.0;

            for (int C = 0; C < outputDim; C++)
            {
                double subSum = 0.0;
                for (int i = 0; i < numOfRules; i++)
                    subSum += (i == rule ?
                        (grad[p] * (1.0 / firingSum - firings[rule] / (firingSum * firingSum))) :
                        (-firings[i] * grad[p] / (firingSum * firingSum))) * z[i].Z[C];


                g += (o[C] - y[C]) * subSum;
            }
            return g;
        }

        public bool isTrainingstoped()
        {
            return isStop;
        }


        public double Error(double[][] x, double[][] y, IList<IRule> ruleBase)
        {
            
            if (x.Length != y.Length)
                throw new Exception("Input and desired output lengths not match");
            if (ruleBase == null || ruleBase.Count == 0)
                throw new Exception("Incorrect rulebase");

            int outputDim = ruleBase[0].Z.Length;
            int numOfRules = ruleBase.Count;

            double globalError = 0.0;

            for (int sample = 0; sample < x.Length; sample++)
            {
                double[] o = ANFIS.Inference(x[sample], ruleBase);
                for (int C = 0; C < outputDim; C++)
                    globalError += Math.Abs(o[C] - y[sample][C]);
            }

            return globalError / x.Length;
        }


        public bool isAdjustingRules()
        {
            return UnknownCaseFaced != null;
        }
    }
}
