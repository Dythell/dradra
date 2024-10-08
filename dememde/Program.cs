var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()  
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseRouting();


List<Order> bd = new List<Order>()
{
    new Order(1, 2022, 12, 5, "тостер", "не_работает", "сгорел", "я", "новая заявка", "",""),
    new Order(2, 2020, 12, 1, "микроволновка", "не_включается", "сломатен", "Петр", "в процессе", "", ""),
};

app.MapGet("/", () => bd);

//app.MapPost("/", (Order ord) => bd.Add(ord));
app.MapPost("/", (Order ord) =>
{
    int newId = bd.Any() ? bd.Max(o => o.Number) + 1 : 1;
    ord.Number = newId;
    bd.Add(ord);
    return Results.Json(ord);
});

app.MapPut("/{number}", (int number, OrderUpdate upd) =>
{
    Order space = bd.Find(ord => ord.Number == number);
    if (space == null)
        return Results.NotFound("нет");

    if (!string.IsNullOrEmpty(upd.Status))
    {
        space.Status = upd.Status;
        if (space.Status.Equals("выполнено", StringComparison.OrdinalIgnoreCase))
        {
            space.CompleteDate = DateTime.Now;
            SendNotification(space);
        }
    }

    if (!string.IsNullOrEmpty(upd.Master))
        space.Master = upd.Master;
    if (!string.IsNullOrEmpty(upd.Comment))
        space.Comment = upd.Comment;
    if (!string.IsNullOrEmpty(upd.ProblemDescription))
        space.ProblemDescription = upd.ProblemDescription;

    return Results.Json(space);
});



app.MapGet("/{number}", (int number) =>
{
    Order order = bd.Find(ord => ord.Number == number);
    if (order == null)
        return Results.NotFound("неть");

    return Results.Json(order);
});

app.MapGet("/search", (string? client, string? master, string? status) =>
{
    var filteredOrders = bd.Where(ord =>
        (string.IsNullOrEmpty(client) || ord.Client.Equals(client, StringComparison.OrdinalIgnoreCase)) &&
        (string.IsNullOrEmpty(master) || ord.Master.Equals(master, StringComparison.OrdinalIgnoreCase)) &&
        (string.IsNullOrEmpty(status) || ord.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
    ).ToList();
    if (filteredOrders.Count == 0)
        return Results.NotFound("неть");

    return Results.Json(filteredOrders);
});

app.MapGet("/stat/completed-application", () =>
{
    var completedApplications = bd.FindAll(ord => ord.Status == "выполнено");
    var count = completedApplications.Count();

    return Results.Json(new
    {
        Count = count,
        Applications = completedApplications
    });
});


app.MapGet("/stat/problem-type", () =>
{
    var problemStatistics = bd
        .GroupBy(order => order.ProblemType)
        .Select(group => new
        {
            ProblemType = group.Key,
            Count = group.Count(),
            Orders = group.ToList()
        })
        .ToList();

    if (problemStatistics.Count == 0)
        return Results.NotFound("неть");

    return Results.Json(problemStatistics);
});


app.MapGet("/stat/time-to-complete", () =>
{
    var completedOrders = bd.Where(ord => ord.CompleteDate.HasValue).ToList();

    if (completedOrders.Count == 0)
        return Results.Json(new { AverageTimeInHours = 0, CompletionTimes = new List<object>() });

    var completionTimes = completedOrders.Select(ord => new
    {
        ord.Number,
        TimeTakenInHours = (int)((ord.CompleteDate.Value - ord.CreateDate).TotalMinutes / 60)
    }).ToList();

    int totalHours = completionTimes.Sum(ct => ct.TimeTakenInHours);
    int averageTimeInHours = totalHours / completedOrders.Count;
    return Results.Json(new
    {
        AverageTimeInHours = averageTimeInHours,
        CompletionTimes = completionTimes
    });
});


app.Run();
void SendNotification(Order order)
{

    Console.WriteLine($"Заказ {order.Number} выполнен. Клиент: {order.Client} Мастер: {order.Master}");
}


class Order
{
    int number;
    int year;
    int month;
    int day;
    string device;
    string problemType;
    string problemDescription;
    string client;
    string status;
    string master;
    string comment;
    public DateTime CreateDate { get; }
    public DateTime? CompleteDate { get; set; }

    public Order(int number, int year, int month, int day, string device, string problemType, string problemDescription, string client, string status, string master, string comment)
    {
        Number = number;
        Day = day;
        Month = month;
        Year = year;
        Device = device;
        ProblemType = problemType;
        ProblemDescription = problemDescription;
        Client = client;
        Status = status;
        Master = master;
        Comment = comment;
        CreateDate = new DateTime(year, month, day);
        CompleteDate = null;
    }

    public int Number { get => number; set => number = value; }
    public int Day { get => day; set => day = value; }
    public int Month { get => month; set => month = value; }
    public int Year { get => year; set => year = value; }
    public string Device { get => device; set => device = value; }
    public string ProblemType { get => problemType; set => problemType = value; }
    public string ProblemDescription { get => problemDescription; set => problemDescription = value; }
    public string Client { get => client; set => client = value; }
    public string Status { get => status; set => status = value; }
    public string Master { get => master; set => master = value; }
    public string Comment { get => comment; set => comment = value; }
}

class OrderUpdate
{
    string status;
    string problemDescription;
    string master;
    string comment;
    DateTime? completeDate;

    public string Status { get => status; set => status = value; }
    public string ProblemDescription { get => problemDescription; set => problemDescription = value; }
    public string Master { get => master; set => master = value; }
    public string Comment { get => comment; set => comment = value; }
    public DateTime? CompleteDate { get => completeDate; set => completeDate = value; }
}