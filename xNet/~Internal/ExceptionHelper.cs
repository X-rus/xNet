using System;

namespace xNet
{
    internal static class ExceptionHelper
    {
        internal static ArgumentException EmptyString(string paramName)
        {
            return new ArgumentException(Resources.ArgumentException_EmptyString, paramName);
        }

        internal static ArgumentOutOfRangeException CanNotBeLess<T>(string paramName, T value) where T : struct
        {
            return new ArgumentOutOfRangeException(paramName, string.Format(
                    Resources.ArgumentOutOfRangeException_CanNotBeLess, value));
        }

        internal static ArgumentOutOfRangeException CanNotBeGreater<T>(string paramName, T value) where T : struct
        {
            return new ArgumentOutOfRangeException(paramName, string.Format(
                    Resources.ArgumentOutOfRangeException_CanNotBeGreater, value));
        } 

        internal static ArgumentException WrongPath(string paramName, Exception innerException = null)
        {
            return new ArgumentException(Resources.ArgumentException_WrongPath, paramName, innerException);
        }

        internal static ArgumentOutOfRangeException WrongTcpPort(string paramName)
        {
            return new ArgumentOutOfRangeException("port", string.Format(
                Resources.ArgumentOutOfRangeException_CanNotBeLessOrGreater, 1, 65535));
        }

        internal static bool ValidateTcpPort(int port)
        {
            if (port < 1 || port > 65535)
            {
                return false;
            }

            return true;
        }
    }
}