using System;
using AOT;
using Unity.Burst;
using Unity.Mathematics;

namespace Utopia.World.BiomeTypes
{
	public enum NoiseBiomeOperation
	{
		Greater, Less
	}

	[BurstCompile]
	public static class NoiseBiomeOperations
	{
		public delegate double Operation(double value, double threshold);

		public static FunctionPointer<Operation> GetOperation(this NoiseBiomeOperation operation)
			=> operation switch
		{
			NoiseBiomeOperation.Greater => GreaterPtr,
			NoiseBiomeOperation.Less => LessPtr,
			_ => throw new ArgumentOutOfRangeException
			(
				nameof(operation), operation,
				$"Unimplemented operation \"{operation}\"."
			)
		};

		[BurstCompile, MonoPInvokeCallback(typeof(Operation))]
		private static double Greater(double value, double threshold) => math.clamp(value - threshold, 0.0f, 1.0f);

		private static readonly FunctionPointer<Operation> GreaterPtr
			= BurstCompiler.CompileFunctionPointer<Operation>(Greater);

		[BurstCompile, MonoPInvokeCallback(typeof(Operation))]
		private static double Less(double value, double threshold)
		{
			value = -value;
			value = Greater(value, threshold);
			return -value;
		}

		private static readonly FunctionPointer<Operation> LessPtr
			= BurstCompiler.CompileFunctionPointer<Operation>(Less);
	}
}