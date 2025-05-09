using System.Text;

namespace MockTest.Exceptions;

public class NotFoundException(string message) : Exception(message);
