using Microsoft.AspNetCore.Mvc;

namespace GuessMyNumber.Controllers
{
    public class GuessMyNumberController : Controller
    {
        private const string SessKeyForRandom = "randomNr";         //Key for random session
        private const string SessKeyForCounter = "gCounter";        //Key for counter session
        private const string SessKeyForHighScore = "highScore";       //Key for highscore session

        private static readonly Random random = new Random();       //Make Random static just in case of likely stupidity

        [HttpGet]
        public IActionResult Index()
        {
            int randomNr = random.Next(1, 101);                     //The random number is born

            HttpContext.Session.SetInt32(SessKeyForRandom, randomNr);   //Storing the number
            HttpContext.Session.SetInt32(SessKeyForCounter, 0);         //Initial setup for counter

            ViewBag.HighScores = GetSessionHighScores();
            return View();
        }

        [HttpPost]
        public IActionResult Index(int? guess)
        {
            if (!guess.HasValue)                                                //Check if the guess is valid
            {
                ViewBag.Message = "Please enter a valid number.";
                ViewBag.HighScores = GetSessionHighScores();
                return View();
            }

            int? randomNumber = HttpContext.Session.GetInt32(SessKeyForRandom); //Retrieve the random number from session
            int? guessCount = HttpContext.Session.GetInt32(SessKeyForCounter);  //Retrieve the guess count from session

            if (randomNumber == null || guessCount == null)
            {
                ViewBag.Message = "Session expired. Please start a new game.";  //Handle the case where session has expired or is not set
                return RedirectToAction("Index");                               //  --not my original idea, just saw it somewhere
            }

            guessCount++;                                                       //Increment the guess count
            HttpContext.Session.SetInt32(SessKeyForCounter, guessCount.Value);  //Update the guess count in session

            if (guess.Value == randomNumber.Value)                              //Check if the guess is correct
            {
                ViewBag.Message = $"Congratulations! You guessed the number in {guessCount} attempts.";
                UpdateHighScores(guessCount.Value);                             //Update high scores
                HttpContext.Session.Remove(SessKeyForRandom);                   //Remove the random number from session
                HttpContext.Session.Remove(SessKeyForCounter);                  //Remove the guess count from session
            }
            else if (guess.Value < randomNumber.Value)
            {
                ViewBag.Message = "Too low! Try again.";                        //Feedback for a guess too low
            }
            else
            {
                ViewBag.Message = "Too high! Try again.";                       //Feedback for a guess too high
            }

            ViewBag.GuessCount = guessCount;                                    //Display the number of guesses
            ViewBag.HighScores = GetSessionHighScores();                        //Display high scores
            return View();
        }

        private List<int> GetSessionHighScores()
        {
            var highScores = new List<int>();
            var cookie = Request.Cookies[SessKeyForHighScore];      //Retrieve the session
            if (!string.IsNullOrEmpty(cookie))
            {
                var scores = cookie.Split(',');                     //Split the session value into individual scores
                foreach (var score in scores)
                {
                    if (int.TryParse(score, out int passed))
                    {
                        highScores.Add(passed);                      //Add each score to the list
                    }
                }
            }
            return highScores;
        }

        private void UpdateHighScores(int guessCount)                               //Update high scores in session
        {
            var highScores = GetSessionHighScores();                                //Retrieve current high scores
            highScores.Add(guessCount);                                             //Add the new score
            highScores = highScores.OrderBy(score => score).Take(10).ToList();      //Sort the scores and keep only top 10
            var cookieValue = string.Join(",", highScores);                         //Join the scores into a single string
            Response.Cookies.Append(SessKeyForHighScore, cookieValue);              //Add the cookie to the response
        }

    }
}
