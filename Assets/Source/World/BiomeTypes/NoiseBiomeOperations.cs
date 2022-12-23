using System;
using AOT;
using Unity.Burst;
using static Unity.Mathematics.math;

namespace Utopia.World.BiomeTypes {
	/// <summary>
	///     A set of operations that can be performed in a <see cref="NoiseBiome" />.
	/// </summary>
	public enum NoiseBiomeOperation {
		/// <summary>
		///     Spawns the biome if greater than the given threshold.
		/// </summary>
		Greater
	}

	/// <summary>
	///     Static container class for the function pointers
	///     and helper methods for the <see cref="NoiseBiomeOperation" /> enum.
	/// </summary>
	[BurstCompile]
	public static class NoiseBiomeOperations {
		/// <summary>
		///     An operation that can be performed in a <see cref="NoiseBiome" />.
		/// </summary>
		/// <param name="value">The value that the noise biome function has just generated.</param>
		/// <param name="threshold">The threshold to compare the <see cref="value" /> to.</param>
		/// <returns>The modified value from the operation.</returns>
		public delegate double Operation(double value, double threshold);

		/// <summary>
		///     Burst compiled function pointer to <see cref="Greater" />.
		/// </summary>
		private static readonly FunctionPointer<Operation> GreaterPtr
			= BurstCompiler.CompileFunctionPointer<Operation>(Greater);

		/// <summary>
		///     Returns the function pointer to the given operation.
		/// </summary>
		/// <param name="operation">
		///     The <see cref="NoiseBiomeOperation" /> instance to get the function pointer to.
		/// </param>
		/// <returns>A Burst-compiled function pointer that performs the given operation.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///     Thrown if a value is given that hasn't had a function implemented / pointer assigned yet.
		/// </exception>
		public static FunctionPointer<Operation> GetOperation(this NoiseBiomeOperation operation) {
			return operation switch {
				NoiseBiomeOperation.Greater => GreaterPtr,
				_ => throw new ArgumentOutOfRangeException
				(
					nameof(operation), operation,
					$"Unimplemented operation \"{operation}\"."
				)
			};
		}

		/// <inheritdoc cref="NoiseBiomeOperation.Greater" />
		/// <param name="value">The value at the given noise sample.</param>
		/// <param name="threshold">The threshold that the value must be greater than.</param>
		/// <returns>
		///     The noise value between 0 and 1,
		///     but only greater than zero if above the given threshold.
		/// </returns>
		[BurstCompile]
		[MonoPInvokeCallback(typeof(Operation))]
		private static double Greater(double value, double threshold) {
			double val = clamp(value - threshold, 0.0f, 1.0f);
			val = unlerp(0.0f, 1.0f - threshold, val);
			return val;
		}
	}
}