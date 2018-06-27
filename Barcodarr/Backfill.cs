using Esendex.TokenBucket;
using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Barcodarr
{
    public class Backfill
    {
        private static RestClient restClientTMDB = new RestClient("https://api.themoviedb.org/");
        private static RestClient restClientCollectorz = new RestClient("https://moviesuggest.collectorz.net/");
        private static ITokenBucket bucket = TokenBuckets.Construct().WithCapacity(40).WithFixedIntervalRefillStrategy(39, TimeSpan.FromSeconds(10)).Build();
        private static ITokenBucket bucketC = TokenBuckets.Construct().WithCapacity(5).WithFixedIntervalRefillStrategy(5, TimeSpan.FromSeconds(1)).Build();
        private static DateTime startDate = DateTime.Now;
        private static ConcurrentDictionary<int, string> movieList = new ConcurrentDictionary<int, string>();
        private static ConcurrentDictionary<string, string> collectorzList = new ConcurrentDictionary<string, string>();
        private static volatile bool end = false;

        public static void Main(string[] args)
        {
            var t1 = Task.Run(async () => { await Runapp(args); });

            t1.Wait();
        }

        private static async Task Runapp(string[] args)
        {
            //3561ec153455c597a64aa5c6827e8e09
            //"https://moviesuggest.collectorz.net/quicksearch.php?query=b"
            //https://api.themoviedb.org/3/discover/movie?api_key=3561ec153455c597a64aa5c6827e8e09&language=en-US&sort_by=popularity.desc&include_adult=false&include_video=false&page=1&year=2017
            
            List<int> yearList = new List<int>();
            for (int year = 2018; year > 2000; year--)
            {
                yearList.Add(year);
            }

            var yearTasks = yearList.Select(year =>
            {
                Task.Run(async () =>
                {
                    int p = 1;
                    var movies = await DiscoverMovies(1, year);
                    int pEnd = movies.total_pages;
                    while (p <= pEnd)
                    {
                        movies = await DiscoverMovies(p, year);
                        var t2 = Task.Run(async () =>
                        {
                            foreach (var movie in movies.results)
                            {
                                Console.WriteLine("  " + movie.title);
                                movieList.AddOrUpdate(movie.id, movie.title, (key, value) => movie.title);
                                var films = await QueryText(movie.title);
                                foreach (var film in films.suggest)
                                {
                                    collectorzList.AddOrUpdate(film.connectName, film.title, (k, v) => film.title);
                                }
                            }
                        });

                        Console.WriteLine($"Year {year} Page {p} Thread {Thread.CurrentThread.ManagedThreadId} Time {(DateTime.Now - startDate).Seconds}");
                        p++;
                    }
                    Console.WriteLine($"Finished Year {year} Total Pages {pEnd} Total movies {movieList.Count}");
                    return year;
                });
            });

            var yearResults = await Task.WhenAll(yearTasks);



        }

        async static Task<CollectorzRoot> QueryText(string q)
        {
            bucketC.Consume(1);
            var restRequest = new RestRequest("/quicksearch.php?query={q}");
            restRequest.AddUrlSegment("q", q);
            restRequest.RequestFormat = DataFormat.Json;
            var obj = await restClientCollectorz.ExecuteTaskAsync(restRequest);

            var d = new RestSharp.Deserializers.JsonDeserializer().Deserialize<CollectorzRoot>(obj);

            return d;
        }

        async static Task<TMDBRoot> DiscoverMovies(int page, int year)
        {

            bucket.Consume(1);
            var restRequest = new RestRequest("/3/discover/movie?api_key=3561ec153455c597a64aa5c6827e8e09&language=en-US&sort_by=popularity.desc&include_adult=false&include_video=false&page={page}&year={year}", RestSharp.Method.GET);
            restRequest.AddUrlSegment("year", year);
            restRequest.AddUrlSegment("page", page);

            var restResult = await restClientTMDB.ExecuteTaskAsync<TMDBRoot>(restRequest, CancellationToken.None);
            if (restResult?.Data?.results == null)
            {
                throw new NullReferenceException();
            }
            return restResult.Data;
        }
    }






    public class CollectorzRoot
    {
        public List<Suggest> suggest { get; set; }
    }

    public class Suggest
    {
        public string id { get; set; }
        public string title { get; set; }
        public string releaseYear { get; set; }
        public string connectName { get; set; }
        public string coverUrl { get; set; }
        public string isTvSeries { get; set; }
        public string isBoxSet { get; set; }
        public string isAdult { get; set; }
    }


    public class TMDBRoot
    {
        public int page { get; set; }
        public int total_results { get; set; }
        public int total_pages { get; set; }
        public List<Result> results { get; set; }
    }

    public class Result
    {
        public int vote_count { get; set; }
        public int id { get; set; }
        public bool video { get; set; }
        public float vote_average { get; set; }
        public string title { get; set; }
        public float popularity { get; set; }
        public string poster_path { get; set; }
        public string original_language { get; set; }
        public string original_title { get; set; }
        public List<int> genre_ids { get; set; }
        public string backdrop_path { get; set; }
        public bool adult { get; set; }
        public string overview { get; set; }
        public string release_date { get; set; }
    }
}