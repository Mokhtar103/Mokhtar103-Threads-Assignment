using OrderProcessing.Data;
using OrderProcessing.Helpers;
using OrderProcessing.Models;
using OrderProcessing.Repositories;
using OrderProcessing.Runner;
using OrderProcessing.Services;
using OrderProcessing.Services.Interfaces;
using System.Diagnostics;

CancellationToken ct = CancellationToken.None;
//await DataSeeder.ResetAsync();

//Console.WriteLine("Seeding completed.");


JsonFileStore fileStore = new();

CustomerRepository customerRepository = new(fileStore);
ProductRepository productRepository = new(fileStore);
CouponRepository couponRepository = new(fileStore);
var orderRepository = new OrderRepository(
    fileStore,
    failSaveForOrderIds: new[] { 13 }); 
IncomingOrderRepository incomingOrderRepository = new(fileStore);

#region Phase 1

//SyncCustomerRepository syncCustomerRepository = new(fileStore);
//SyncProductRepository syncProductRepository = new(fileStore);
//SyncCouponRepository syncCouponRepository = new(fileStore);

//SyncOrderValidator syncValidator = new(
//    syncCustomerRepository,
//    syncProductRepository,
//    syncCouponRepository);

//OrderValidator asyncValidator = new(
//    customerRepository,
//    productRepository,
//    couponRepository,
//    orderRepository);

//Phase1Runner runner = new(
//    fileStore,
//    incomingOrderRepository,
//    orderRepository,
//    syncValidator,
//    asyncValidator);

//await runner.RunAsync(CancellationToken.None);

#endregion


#region Phase 2 - 8.2 Testing Methods on Order #1

//PricingService pricingService = new(
//    productRepository,
//    customerRepository,
//    couponRepository,
//    new[] { 7 });

//OrderPricer orderPricer = new(pricingService);

//await DataSeeder.ResetAsync(ct);

//List<Order> incomingOrders = await incomingOrderRepository.GetAllAsync(ct);

//Order? originalOrder = null;

//foreach (Order order in incomingOrders)
//{
//    if (order.Id == 1)
//    {
//        originalOrder = order;
//        break;
//    }
//}

//if (originalOrder is null)
//{
//    throw new InvalidOperationException("Order #1 was not found");
//}

//Order sequentialOrder = CloneOrder(originalOrder);
//Order concurrentOrder = CloneOrder(originalOrder);

//Stopwatch sequentialStopwatch = Stopwatch.StartNew();

//await orderPricer.PriceOrderSequentialAsync(sequentialOrder, ct);

//sequentialStopwatch.Stop();

//Stopwatch concurrentStopwatch = Stopwatch.StartNew();

//await orderPricer.PriceOrderAsync(concurrentOrder, ct);

//concurrentStopwatch.Stop();

//Console.WriteLine();
//Console.WriteLine("===== Pricing Performance =====");
//Console.WriteLine(
//    $"Sequential: {sequentialStopwatch.Elapsed.TotalSeconds:F2} seconds");
//Console.WriteLine(
//    $"Concurrent: {concurrentStopwatch.Elapsed.TotalSeconds:F2} seconds");

//Console.WriteLine();
//Console.WriteLine(
//    $"Order #1 subtotal: {concurrentOrder.SubTotal:F2}");
//Console.WriteLine(
//    $"Order #1 discount: {concurrentOrder.Discount:F2}");
//Console.WriteLine(
//    $"Order #1 shipping: {concurrentOrder.ShippingCost:F2}");
//Console.WriteLine(
//    $"Order #1 total: {concurrentOrder.TotalAmount:F2}");

//static Order CloneOrder(Order source)
//{
//    Order copy = new()
//    {
//        Id = source.Id,
//        CustomerId = source.CustomerId,
//        CouponCode = source.CouponCode,
//        SubTotal = 0m,
//        Discount = 0m,
//        ShippingCost = 0m,
//        TotalAmount = 0m,
//        Status = source.Status,
//        CreatedAt = source.CreatedAt,
//        FailureReason = source.FailureReason,
//        TransactionId = source.TransactionId
//    };

//    foreach (OrderItem item in source.Items)
//    {
//        copy.Items.Add(new OrderItem
//        {
//            ProductId = item.ProductId,
//            Quantity = item.Quantity,
//            UnitPrice = 0m
//        });
//    }

//    return copy;
//}

#endregion

#region Phase 2 - 8.3 Concurrent orders

//await DataSeeder.ResetAsync(CancellationToken.None);

//using PhaseRunCancellation runCancellation = new();

//RaceConditionDemo demo = new(fileStore, productRepository);

//await demo.RunAsync(runCancellation.Token);

#endregion

#region Phase 2 - 8.4, 8.5 Processing Orders Concurrently & Generating Report

//OrderValidator orderValidator = new(
//    customerRepository,
//    productRepository,
//    couponRepository,
//    orderRepository);


//PricingService pricingService = new(
//    productRepository,
//    customerRepository,
//    couponRepository,
//    new[] { 7 });

//OrderPricer orderPricer = new(pricingService);

//InvoiceGenerator invoiceGenerator = new();



//OrderProcessorV2 orderProcessor = new(
//    productRepository,
//    orderRepository,
//    orderPricer,
//    invoiceGenerator);

//RaceConditionDemo raceConditionDemo = new(fileStore, productRepository);

//ReportGenerator reportGenerator = new(
//    orderRepository,
//    productRepository);

//Phase2Runner phase2Runner = new(
//    fileStore,
//    incomingOrderRepository,
//    orderRepository,
//    orderValidator,
//    raceConditionDemo,
//    orderPricer,
//    orderProcessor,
//    reportGenerator);

//using PhaseRunCancellation runCancellation = new();

//await phase2Runner.RunAsync(runCancellation.Token);

#endregion

#region Phase 3 - 9.3 Order Processing

//var customerService = new CustomerService(customerRepository);
//var inventoryService = new InventoryService(productRepository);

//var pricingService = new PricingService(
//    productRepository,
//    customerRepository,
//    couponRepository,
//    failShippingForOrderIds: Array.Empty<int>());

//IPaymentGateway paymentGateway = new PaymentGateway(
//    fileStore,
//    timeoutOrderIds: new[] { 5 },
//    declineOrderIds: new[] { 8 });

//IEmailService emailService = new EmailService(
//    fileStore,
//    failAttempts: new Dictionary<int, int>
//    {
//        [3] = 2,
//        [17] = int.MaxValue
//    });

//var orderProcessor = new OrderProcessor(
//    customerService,
//    inventoryService,
//    pricingService,
//    paymentGateway,
//    orderRepository,
//    emailService,
//    productRepository,
//    fileStore);

//var phase3Runner = new Phase3Runner(
//    incomingOrderRepository,
//    orderRepository,
//    orderProcessor);

//await phase3Runner.RunAsync(CancellationToken.None);

#endregion

#region Phase 3 - 9.6 Sync vs async comparison

var comparisonRunner = new SyncAsyncComparisonRunner(
    fileStore,
    incomingOrderRepository);

await comparisonRunner.RunAsync(
    createAsyncProcessor: () =>
    {
        var customerService = new CustomerService(customerRepository);
        var inventoryService = new InventoryService(productRepository);

        var pricingService = new PricingService(
            productRepository,
            customerRepository,
            couponRepository,
            failShippingForOrderIds: Array.Empty<int>(),
            failuresEnabled: false);

        var paymentGateway = new PaymentGateway(
            fileStore,
            timeoutOrderIds: Array.Empty<int>(),
            declineOrderIds: Array.Empty<int>(),
            failuresEnabled: false);

        var orderRepository = new OrderRepository(
            fileStore,
            failSaveForOrderIds: Array.Empty<int>(),
            failuresEnabled: false);

        var emailService = new EmailService(
            fileStore,
            failAttempts: new Dictionary<int, int>(),
            failuresEnabled: false);

        return new OrderProcessor(
            customerService,
            inventoryService,
            pricingService,
            paymentGateway,
            orderRepository,
            emailService,
            productRepository,
            fileStore);
    },
    ct: CancellationToken.None);

#endregion

