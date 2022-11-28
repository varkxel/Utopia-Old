using System;
using AOT;
using Unity.Burst;

namespace Utopia.World.Biomes {
	public enum ThresholdOperation
	{
		Greater, Less,
		GreaterEqual, LessEqual
	}
	
	public static class ThresholdOperations
	{
		public delegate bool Delegate(double val, double threshold);
		
		/// <summary>
		/// Gets the function pointer for the threshold operation.
		/// </summary>
		/// <param name="operation">ThresholdOperation Enum</param>
		/// <returns>Function pointer of the operation.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown if there is no function for the given operation.
		/// </exception>
		public static FunctionPointer<Delegate> GetOperation(this ThresholdOperation operation) {
			return operation switch {
				ThresholdOperation.Greater => GreaterPtr,
				ThresholdOperation.GreaterEqual => GreaterEqualPtr,
				ThresholdOperation.Less => LessPtr,
				ThresholdOperation.LessEqual => LessEqualPtr,
				_ => throw new ArgumentOutOfRangeException
				(
					nameof(operation), operation,
					$"Unimplemented operation \"{operation}\"."
				)
			};
		}
		
		#region Function Pointers
		
		[BurstCompile, MonoPInvokeCallback(typeof(Delegate))]
		private static bool Greater(double val, double threshold) => val > threshold;
		
		private static readonly FunctionPointer<Delegate> GreaterPtr
			= BurstCompiler.CompileFunctionPointer<Delegate>(Greater);
		
		[BurstCompile, MonoPInvokeCallback(typeof(Delegate))]
		private static bool Less(double val, double threshold) => val < threshold;
		
		private static readonly FunctionPointer<Delegate> LessPtr
			= BurstCompiler.CompileFunctionPointer<Delegate>(Less);
		
		[BurstCompile, MonoPInvokeCallback(typeof(Delegate))]
		private static bool GreaterEqual(double val, double threshold) => val >= threshold;
		
		private static readonly FunctionPointer<Delegate> GreaterEqualPtr
			= BurstCompiler.CompileFunctionPointer<Delegate>(GreaterEqual);
		
		[BurstCompile, MonoPInvokeCallback(typeof(Delegate))]
		private static bool LessEqual(double val, double threshold) => val <= threshold;
		
		private static readonly FunctionPointer<Delegate> LessEqualPtr
			= BurstCompiler.CompileFunctionPointer<Delegate>(LessEqual);
		
		#endregion
	}
}