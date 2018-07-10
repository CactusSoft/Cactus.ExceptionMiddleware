namespace Cactus.Owin
{
    public class ExceptionResponse
    {
        /// <summary>
        /// Exception message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Exception deatails data
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message">Exception message</param>
        public ExceptionResponse(string message)
        {
            Message = message;
        }
    }
}
