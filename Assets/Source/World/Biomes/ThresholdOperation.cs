using System;
using AOT;
using Unity.Burst;

namespace Utopia.World.Biomes {
	public enum ThresholdOperation
	{
		Greater, Less,
		GreaterEqual, LessEqual
	}

	public static class ThresholdOperationExtensions
	{
		public delegate bool ThresholdDelegate(double val, double threshold);
		
		public static FunctionPointer<ThresholdDelegate> GetOperation(this ThresholdOperation op) {
			return op switch {
				ThresholdOperation.Greater => GreaterPtr,
				ThresholdOperation.GreaterEqual => GreaterEqualPtr,
				ThresholdOperation.Less => LessPtr,
				ThresholdOperation.LessEqual => LessEqualPtr,
				_ => throw new ArgumentOutOfRangeException
				(
					nameof(op), op,
					$"Unimplemented operation \"{op}\"."
				)
			};
		}
		
		[BurstCompile, MonoPInvokeCallback(typeof(ThresholdDelegate))]
		private static bool Greater(double val, double threshold) => val > threshold;
		
		private static readonly FunctionPointer<ThresholdDelegate> GreaterPtr
			= BurstCompiler.CompileFunctionPointer<ThresholdDelegate>(Greater);
		
		[BurstCompile, MonoPInvokeCallback(typeof(ThresholdDelegate))]
		private static bool Less(double val, double threshold) => val < threshold;
		
		private static readonly FunctionPointer<ThresholdDelegate> LessPtr
			= BurstCompiler.CompileFunctionPointer<ThresholdDelegate>(Less);
		
		[BurstCompile, MonoPInvokeCallback(typeof(ThresholdDelegate))]
		private static bool GreaterEqual(double val, double threshold) => val >= threshold;
		
		private static readonly FunctionPointer<ThresholdDelegate> GreaterEqualPtr
			= BurstCompiler.CompileFunctionPointer<ThresholdDelegate>(GreaterEqual);
		
		[BurstCompile, MonoPInvokeCallback(typeof(ThresholdDelegate))]
		private static bool LessEqual(double val, double threshold) => val <= threshold;
		
		private static readonly FunctionPointer<ThresholdDelegate> LessEqualPtr
			= BurstCompiler.CompileFunctionPointer<ThresholdDelegate>(LessEqual);
	}
}