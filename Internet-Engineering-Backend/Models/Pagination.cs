namespace Internet_Engineering_Backend.Models;

public class Pagination<T>
{
	public required int PerPage { get; set; }
	public required int Page { get; set; }
	public required int TotalPages { get; set; }
	public required List<T> Data { get; set; }

	public static Pagination<T> Paginate(int page, int perPage, List<T> data)
	{
		perPage = Math.Max(perPage, 1);
		var totalPages = (data.Count + perPage - 1) / perPage;

		if (totalPages == 0)
			return new Pagination<T>
			{
				Page = 0,
				PerPage = perPage,
				TotalPages = 0,
				Data = new List<T>()
			};

		page = Math.Clamp(page, 1, totalPages);

		return new Pagination<T>
		{
			Page = page,
			PerPage = perPage,
			TotalPages = totalPages,
			Data = data.Skip((page - 1) * perPage).Take(perPage).ToList()
		};
	}
}
