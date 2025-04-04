﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// A RepeatBehavior describes how a Timeline object may repeat its simple duration.
    /// There are three types of RepeatBehavior behaviors: IterationCount, RepeatDuration, and Forever.
    /// <para>An IterationCount RepeatBehavior specifies the number of times the simple duration of a Timeline will
    /// be repeated. An iteration count of 0.5 means the Timeline will only be active for half
    /// of its simple duration and will only reach 50% progress. An iteration count of 1.0 is the
    /// default and means a Timeline will be active for exactly one of its simple durations. An
    /// iteration count of 2.0 means a Timeline will run twice, or repeat its simple duration 
    /// once after its initial simple duration.</para>
    /// <para>A RepeatDuration RepeatBehavior specifies the amount of time that a Timeline will repeat.
    /// For instance if a Timeline has a simple Duration value of 1 second and a RepeatBehavior with a
    /// RepeatDuration value of 2.5 seconds, then it will run for 2.5 iterations.</para>
    /// <para>A Forever RepeatBehavior specifies that a Timeline will repeat forever.</para>
    /// </summary>
    [TypeConverter(typeof(RepeatBehaviorConverter))]
    public readonly struct RepeatBehavior : IFormattable
    {
        private readonly double _iterationCount;
        private readonly TimeSpan _repeatDuration;
        private readonly RepeatBehaviorType _type;

        #region Constructors

        /// <summary>
        /// Creates a new RepeatBehavior that represents and iteration count.
        /// </summary>
        /// <param name="count">The number of iterations specified by this RepeatBehavior.</param>
        public RepeatBehavior(double count)
        {
            if (double.IsInfinity(count) || double.IsNaN(count) || count < 0.0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.Timing_RepeatBehaviorInvalidIterationCount, count));

            _repeatDuration = TimeSpan.Zero;
            _iterationCount = count;
            _type = RepeatBehaviorType.IterationCount;
        }

        /// <summary>
        /// Creates a new RepeatBehavior that represents a repeat duration for which a Timeline will repeat
        /// its simple duration.
        /// </summary>
        /// <param name="duration">A TimeSpan representing the repeat duration specified by this RepeatBehavior.</param>
        public RepeatBehavior(TimeSpan duration)
        {
            if (duration < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(duration), SR.Format(SR.Timing_RepeatBehaviorInvalidRepeatDuration, duration));

            _iterationCount = 0.0;
            _repeatDuration = duration;
            _type = RepeatBehaviorType.RepeatDuration;
        }

        /// <summary>
        /// Private constructor, serves for creation of <see cref="RepeatBehavior.Forever"/> only.
        /// </summary>
        /// <param name="behaviorType">Only <see cref="RepeatBehaviorType.Forever"/> value is permitted.</param>
        private RepeatBehavior(RepeatBehaviorType behaviorType)
        {
            Debug.Assert(behaviorType == RepeatBehaviorType.Forever);

            _type = behaviorType;
        }

        #endregion // Constructors

        #region Properties

        /// <summary>
        /// Indicates whether this RepeatBehavior represents an iteration count.
        /// </summary>
        /// <value>True if this RepeatBehavior represents an iteration count; otherwise false.</value>
        public bool HasCount
        {
            get
            {
                return _type == RepeatBehaviorType.IterationCount;
            }
        }

        /// <summary>
        /// Indicates whether this RepeatBehavior represents a repeat duration.
        /// </summary>
        /// <value>True if this RepeatBehavior represents a repeat duration; otherwise false.</value>
        public bool HasDuration
        {
            get
            {
                return _type == RepeatBehaviorType.RepeatDuration;
            }
        }

        /// <summary>
        /// Returns the iteration count specified by this RepeatBehavior.
        /// </summary>
        /// <value>The iteration count specified by this RepeatBehavior.</value>
        /// <exception cref="InvalidOperationException">Thrown if this RepeatBehavior does not represent an iteration count.</exception>
        public double Count
        {
            get
            {
                if (!HasCount)
                    throw new InvalidOperationException(SR.Format(SR.Timing_RepeatBehaviorNotIterationCount, this));

                return _iterationCount;
            }
        }

        /// <summary>
        /// Returns the repeat duration specified by this RepeatBehavior.
        /// </summary>
        /// <value>A TimeSpan representing the repeat duration specified by this RepeatBehavior.</value>
        /// <exception cref="InvalidOperationException">Thrown if this RepeatBehavior does not represent a repeat duration.</exception>
        public TimeSpan Duration
        {
            get
            {
                if (!HasDuration)
                    throw new InvalidOperationException(SR.Format(SR.Timing_RepeatBehaviorNotRepeatDuration, this));

                return _repeatDuration;
            }
        }

        /// <summary>
        /// Creates and returns a <see cref="RepeatBehavior"/> that indicates that a <see cref="Timeline"/>
        /// should repeat its simple duration forever.
        /// </summary>
        /// <value>A <see cref="RepeatBehavior"/> that indicates that a <see cref="Timeline"/>
        /// should repeat its simple duration forever.</value>
        public static RepeatBehavior Forever
        {
            get
            {
                return new RepeatBehavior(RepeatBehaviorType.Forever);
            }
        }

        #endregion // Properties

        #region Methods

        /// <summary>
        /// Indicates whether the specified Object is equal to this RepeatBehavior.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true if value is a RepeatBehavior and is equal to this instance; otherwise false.</returns>
        public override bool Equals(object value)
        {
            return value is RepeatBehavior behavior && Equals(behavior);
        }

        /// <summary>
        /// Indicates whether the specified RepeatBehavior is equal to this RepeatBehavior.
        /// </summary>
        /// <param name="repeatBehavior">A RepeatBehavior to compare with this RepeatBehavior.</param>
        /// <returns>true if repeatBehavior is equal to this instance; otherwise false.</returns>
        public bool Equals(RepeatBehavior repeatBehavior)
        {
            if (_type == repeatBehavior._type)
            {
                switch (_type)
                {
                    case RepeatBehaviorType.Forever:

                        return true;

                    case RepeatBehaviorType.IterationCount:

                        return _iterationCount == repeatBehavior._iterationCount;

                    case RepeatBehaviorType.RepeatDuration:

                        return _repeatDuration == repeatBehavior._repeatDuration;

                    default:

                        Debug.Fail("Unhandled RepeatBehaviorType");
                        return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Indicates whether the specified RepeatBehaviors are equal to each other.
        /// </summary>
        /// <param name="repeatBehavior1"></param>
        /// <param name="repeatBehavior2"></param>
        /// <returns>true if repeatBehavior1 and repeatBehavior2 are equal; otherwise false.</returns>
        public static bool Equals(RepeatBehavior repeatBehavior1, RepeatBehavior repeatBehavior2)
        {
            return repeatBehavior1.Equals(repeatBehavior2);
        }

        /// <summary>
        /// Generates a hash code for this RepeatBehavior.
        /// </summary>
        /// <returns>A hash code for this RepeatBehavior.</returns>
        public override int GetHashCode()
        {
            switch (_type)
            {
                case RepeatBehaviorType.IterationCount:

                    return _iterationCount.GetHashCode();

                case RepeatBehaviorType.RepeatDuration:

                    return _repeatDuration.GetHashCode();

                case RepeatBehaviorType.Forever:

                    // We try to choose an unlikely hash code value for Forever.
                    // All Forever instances need to return the same hash code value.
                    return int.MaxValue - 42;

                default:

                    Debug.Fail("Unhandled RepeatBehaviorType");
                    return base.GetHashCode();
            }
        }

        /// <summary>
        /// Creates a string representation of this RepeatBehavior based on the current culture.
        /// </summary>
        /// <returns>A string representation of this RepeatBehavior based on the current culture.</returns>
        public override string ToString()
        {
            return InternalToString(null, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        public string ToString(IFormatProvider formatProvider)
        {
            return InternalToString(null, formatProvider);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        string IFormattable.ToString(string format, IFormatProvider formatProvider)
        {
            return InternalToString(format, formatProvider);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        internal string InternalToString(string format, IFormatProvider formatProvider)
        {
            switch (_type)
            {
                case RepeatBehaviorType.Forever:

                    return "Forever";

                case RepeatBehaviorType.IterationCount:

                    StringBuilder sb = new StringBuilder();

                    sb.AppendFormat(
                        formatProvider,
                        "{0:" + format + "}x",
                        _iterationCount);

                    return sb.ToString();

                case RepeatBehaviorType.RepeatDuration:

                    return _repeatDuration.ToString();

                default:

                    Debug.Fail("Unhandled RepeatBehaviorType.");
                    return null;
            }
        }

        #endregion // Methods

        #region Operators

        /// <summary>
        /// Indicates whether the specified RepeatBehaviors are equal to each other.
        /// </summary>
        /// <param name="repeatBehavior1"></param>
        /// <param name="repeatBehavior2"></param>
        /// <returns>true if repeatBehavior1 and repeatBehavior2 are equal; otherwise false.</returns>
        public static bool operator ==(RepeatBehavior repeatBehavior1, RepeatBehavior repeatBehavior2)
        {
            return repeatBehavior1.Equals(repeatBehavior2);
        }

        /// <summary>
        /// Indicates whether the specified RepeatBehaviors are not equal to each other.
        /// </summary>
        /// <param name="repeatBehavior1"></param>
        /// <param name="repeatBehavior2"></param>
        /// <returns>true if repeatBehavior1 and repeatBehavior2 are not equal; otherwise false.</returns>
        public static bool operator !=(RepeatBehavior repeatBehavior1, RepeatBehavior repeatBehavior2)
        {
            return !repeatBehavior1.Equals(repeatBehavior2);
        }

        #endregion // Operators

        /// <summary>
        /// An enumeration of the different types of RepeatBehavior behaviors.
        /// </summary>
        private enum RepeatBehaviorType
        {
            IterationCount,
            RepeatDuration,
            Forever
        }
    }
}
