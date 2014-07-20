using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.IO;

namespace WAYWOrithm {
	class JS_FPInfo {
		public string browser;
	}

	class JS_FPAuthor {
		public string name;
		public JS_FPInfo info;
	}

	class JS_FPPost {
		public uint thread;
		public uint page;
		public JS_FPAuthor author;
		public string type;
		public ulong date;
		public string number;
		public string id;
		public string content;
		public string sanitized_content;
		public Dictionary<string, int> ratings;

		public static JS_FPPost Parse(string JSON) {
			return new JavaScriptSerializer().Deserialize<JS_FPPost>(JSON);
		}
	}

	// Actual algorithm
	enum Rating {
		Agree,
		Disagree,
		Funny,
		Winner,
		Zing,
		Informative,
		Friendly,
		Useful,
		ProgrammingKing,
		Gaybow,
		Artistic,
		Late,
		Dumb
	}

	class FPPost : IComparable {
		public static Dictionary<Rating, float> Weights = new Dictionary<Rating, float> {
			{Rating.ProgrammingKing, 100},
			{Rating.Winner, 90},
			{Rating.Useful, 10},
			{Rating.Artistic, 10},
			{Rating.Agree, 2},
			{Rating.Dumb, 0.9f}
		};

		public static float GetWeight(Rating R) {
			if (Weights.ContainsKey(R))
				return Weights[R];
			return 1;
		}

		public ulong ID;
		public string AuthorName;
		public string Content, Sanitized;
		public Dictionary<Rating, ushort> Ratings = new Dictionary<Rating, ushort>();

		public int GetCount(Rating R) {
			if (Ratings.ContainsKey(R))
				return Ratings[R];
			return 0;
		}

		public FPPost(ulong ID, string Author, string Content, string Sanitized, Dictionary<Rating, ushort> Ratings) {
			this.ID = ID;
			this.Ratings = Ratings;
			AuthorName = Author;
			this.Content = Content;
			this.Sanitized = Sanitized;
		}

		public FPPost(JS_FPPost P) {
			this.ID = ulong.Parse(P.id);
			this.AuthorName = P.author.name;
			this.Content = P.content.Trim();
			this.Sanitized = P.sanitized_content.Replace("\r", "").Trim();

			foreach (var I in P.ratings) {
				Rating R;

				switch (I.Key) {
					case "agree":
						R = Rating.Agree;
						break;

					case "disagree":
						R = Rating.Disagree;
						break;

					case "funny":
						R = Rating.Funny;
						break;

					case "winner":
						R = Rating.Winner;
						break;

					case "zing":
						R = Rating.Zing;
						break;

					case "informative":
						R = Rating.Informative;
						break;

					case "friendly":
						R = Rating.Friendly;
						break;

					case "useful":
						R = Rating.Useful;
						break;

					case "programming_king":
						R = Rating.ProgrammingKing;
						break;

					case "optimistic":
						R = Rating.Gaybow;
						break;

					case "artistic":
						R = Rating.Artistic;
						break;

					case "late":
						R = Rating.Late;
						break;

					case "dumb":
						R = Rating.Dumb;
						break;

					default:
						throw new Exception(string.Format("Unknown rating: {0}", I.Key));
				}

				Ratings.Add(R, (ushort)I.Value);
			}
		}

		static Tuple<int, float, float> Cmp(object X, object Y, Rating R) {
			float XX = ((FPPost)X).GetCount(R) * GetWeight(R);
			float YY = ((FPPost)Y).GetCount(R) * GetWeight(R);

			int CmpRes = 0;

			if (XX > YY)
				CmpRes = -1;
			if (XX < YY)
				CmpRes = 1;

			return new Tuple<int, float, float>(CmpRes, XX, YY);
		}

		public int CompareTo(object Y) {
			var Winners = Cmp(this, Y, Rating.Winner);
			var PKings = Cmp(this, Y, Rating.ProgrammingKing);
			var Useful = Cmp(this, Y, Rating.Useful);

			var XX = Winners.Item2 + PKings.Item2 + Useful.Item2;
			var YY = Winners.Item3 + PKings.Item3 + Useful.Item3;

			if (XX > YY)
				return -1;
			if (XX < YY)
				return 1;
			return 0;
		}

		public override string ToString() {
			StringBuilder SB = new StringBuilder();
			SB.Append(ID);
			SB.Append(" >> ");
			SB.AppendLine(AuthorName);
			SB.AppendLine(Content);
			return SB.ToString();
		}
	}

	class FPPostList {
		FPPost[] Posts;

		public FPPostList(FPPost[] Posts) {
			this.Posts = Posts;
		}

		public static bool IsContent(string Content, string Sane) {
			bool R = Content.Contains("<iframe");

			string ImgStr = "<img src=\"";
			string ImgFPStr = ImgStr + "/fp";

			for (int i = 0; i < Content.Length; i++)
				if (i + ImgStr.Length > Content.Length || i + ImgFPStr.Length > Content.Length)
					break;
				else
					if (Content.Substring(i, ImgStr.Length) == ImgStr && Content.Substring(i, ImgFPStr.Length) != ImgFPStr) {
						R = true;
						break;
					}

			return R;
		}

		public static bool IsContent(FPPost P) {
			return IsContent(P.Content, P.Sanitized);
		}

		public FPPost[] GetHighlights() {
			List<FPPost> HLights = new List<FPPost>();

			Array.Sort(Posts);
			int i = 0;
			while (i < Posts.Length) {
				var Post = Posts[i];
				if (IsContent(Post))
					HLights.Add(Posts[i]);
				i++;
			}

			return HLights.ToArray();
		}

		public FPPost GetMostHighlighted() {
			var HL = GetHighlights();
			if (HL.Length > 0)
				return HL[0];
			return null;
		}
	}

	class Program {
		static void Main(string[] args) {
			Console.Title = "WAYWOrithm";

			string[] PostDirs = Directory.GetFiles("pages", "*.json", SearchOption.AllDirectories);
			FPPost[] Posts = new FPPost[PostDirs.Length];

			for (int i = 0; i < PostDirs.Length; i++)
				Posts[i] = new FPPost(JS_FPPost.Parse(File.ReadAllText(PostDirs[i])));

			Dictionary<string, List<FPPost>> UserPosts = new Dictionary<string, List<FPPost>>();
			foreach (var P in Posts) {
				if (!UserPosts.ContainsKey(P.AuthorName))
					UserPosts.Add(P.AuthorName, new List<FPPost>());
				UserPosts[P.AuthorName].Add(P);
			}

			List<FPPost> HighlightPosts = new List<FPPost>();
			foreach (var UsrPosts in UserPosts) {
				var MostHl = new FPPostList(UsrPosts.Value.ToArray()).GetMostHighlighted();
				if (MostHl != null)
					HighlightPosts.Add(MostHl);
			}
			Posts = new FPPostList(HighlightPosts.ToArray()).GetHighlights();

			List<string> IDs = new List<string>();
			for (int i = 0; i < (Posts.Length < 25 ? Posts.Length : 25); i++)
				IDs.Add(Posts[i].ID.ToString());
			Console.WriteLine(String.Join(",", IDs.ToArray()));
		}
	}
}