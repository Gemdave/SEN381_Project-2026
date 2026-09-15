# Assignment 2
> Group I  
> 2026  
> Participating Members:  
> * Gerald Enright 577830  
> * Keletso Marota 601632  
> * Mogau Malope 600192  
---
## Task 1
### Design Problem 
CivicConnect has a controlled service-request lifecycle. A request may be submitted, assigned or accepted, updated, rejected, resolved and closed, with different actions being appropriate at different stages.  
This is a problem because implementation could have the request status represented as an enumeration (enum) alongside methods that are based on the enum values. For small simple transitions this may be reasonable. However, the design becomes difficult if multiple behaviors depend on the current status.  
If those rules become distributed across multiple methods containing repeated status checks, changes to the lifecycle can require changes in several locations.  

**Coupling and cohesion.**  
Coupling is the degree of interdependence between modules or classes: a module is coupled to another if a change in one forces a change in the other.  
Cohesion concerns how closely related the responsibilities within a software element are.  
In the case for CivicConnect coupling will be determined by how many parts of the system need to know the detailed rules associated with every request status. While cohesion will be determined by wether each component contains their own state-specific behavior rules. This is important to remember as development continues, because a system that is over-coupled will be difficult to test or fix should there be an issue. While a system that has low cohesion will be difficult to maintain in the long run, because behavior rules are scattered between components.   

**Relevant SOLID principles.**  
- **Single Responsibility Principle (SRP):** a class should have only one reason to change (Stevenson; 2018). When a class mixes business logic with unrelated concerns, it has more than one reason to change and cohesion can decline. Therefore state-specific behavior should be separated to provide a way of keeping individual components more focused However this does not mean that each the class should be responsible for only one thing.  
- **Open/Closed Principle (OCP):** software entities should be open for extension but closed for modification (Plösch; 2016). A design that must be edited every time a new variant is introduced violates OCP and increases regression risk on code that was already tested.
- **Liskov Substitution Principle (LSP):** Subtypes should be usable wherever their base type is expected without breaking the program's correctness. Using different types of service requests or user roles represented through inheritance, each subtype could honour behavior expected from the parent abstraction.
- **Interface Segregation Principle (ISP):** Clients should not be forced to depend on methods they do not need. Instead of creating one large interface to contain many operations simultaneously, focus should be switched to smaller focus groups.
- **Dependency Inversion Principle (DIP):** High-level modules should depend on abstractions rather than concrete low-level implementations. The request-management logic could depend on a abstracted component rather than directly depending on email or SMS implementation.  

**Problems Identified**  
- **Problem 1:** **Reacting to a service-request state change.** When the state of a request is changed several independent things should happen: The requester needs feedback, a history entry should be created, and staff views should reflect the change. If an individual class handles all these steps for its own status update method then the class takes on more responsibilities than what was originally intended and every new reaction will require editing and re-testing the already working code (Tiwari; 2018).  
- **Problem 2:** **Creating and validating category-specific requests.** There should be a supported, controlled category mechanism and each category plausibly needs different fields and validation. If this is created as one creation routine containing a chain of decision statements keyed on category, the routine must be reopened every time a category is added or has a rule change, and a change aimed at one category risks breaking another - a direct OCP violation.

### Alternatives For Each problem
Literature emphasizes that design patterns should be selected according to the characteristics of the problem rather than applied automatically (Naghdipour; 2023). There are also situations identified where pattern structures can introduce undesirable design smells, demonstrating that the use of a pattern does not guarantee a better design (Almadi; 2021).  
**Problem 1**  
- **Alternative A: Centralized conditional approach**  
A straightforward solution is to represent the request state using an enumeration or similar value and keep lifecycle behavior within a service or request-management. In essence this means that each service will get a status value from an enum and the service handler will manage what needs to be done based on thee status value. This approach is most appropriate when the lifecycle is small, stable and straightforward.  
| Advantages | Limitations |
| :--- | :--- |
| The main advantage is simplicity, as there are few classes and relationships, making the initial design easier to understand. | The main risk is that the central service grows as state-dependant behavior increases. |
| Also makes control flow explicit where a developer investigating a lifecycle operation can find all the relevant rules in one location. | As additional states and operations are introduced the conditional logic can become complicated, where changes to one state may require modifying a singular large file that also handles rules for other states. |
| Testing can also be straightforward when the number of states and transitions is small, by providing a request state and verifying if the operation is accepted or not. | Cohesion can be reduced as changes to one lifecycle are likely to affect unrelated behavior. | 

- **Alternative B: State-Oriented design**  
Another solution is to represent the behavior associated with each lifecycle state separately. This means that the service will still get a status value from an enum and the service handler will call the request rules from a separately stored file for each state. This approach is particularly relevant when the behavior of the request changes substantially according to its state.  
Research on state-based software design demonstrates that state-oriented representations can explicitly associate responsibilities and transitions with states, supporting the separation of state-specific behavior (Nikitin; 2023).
| Advantages | Limitations |
| :--- | :--- |
| The main advantage is the separation of state-dependant behavior. Each state can have a relatively focused responsibility, reducing the amount of state specific logic that a central component needs to maintain. | The main disadvantage is the additional complexity, as the system would move away from one service containing all lifecycle rules to a system with state abstraction, multiple concrete state classes, relationships between requests and the current state, transition logic, and additional tests and documentation |
| This also makes introducing more lifecycle states a simple matter as new behavior can be introduced in an isolated manner rather than in a increasingly large conditional structure. | There is a risk as development needs to understand and maintain a complex architecture, where responsibilities are technically separated but unnecessary abstraction may reduce practical maintainability.
| The design also improves test isolation as individual state behaviors can be tested separately. | 


**Comparison of the Alternatives**
| Criterion | Centralized Conditionals | State-oriented design |
| :--- | :--- | :--- |
| Initial simplicity | HIGH | MODERATE/LOW |
| Number of classes | LOW | HIGH |
| Lifecycle behavior cohesion | Can decrease as complexity grows | Potentially High |
| Coupling | Initially Low | More relationships between state objects |
| Extensibility | Can become difficult as states increase | Better suited for evolving state behavior |
| Testability | Straightforward for small lifecycle | Can isolate individual state behaviors |
| Understandability | Good when lifecycle is simple | Good when lifecycle is complex, but more concepts need to be understood |
| Risk of over-engineering | LOW | HIGH |
| Suitable when | Few stable states and simple rules | Many states or substantial state specific behavior |

**Problem 2**  
- **Alternative A: Factory-Based Request Creation**  
The factory-based approach encapsulates the object-creation decision so that callers do not need to know how a particular category request is constructed. This approach lets the caller interact with the factory mechanism that handles the category rather than calling a category-specific constructor. Particularly useful in situations where categories represent genuinely different request types with different construction requirements.  
| Advantages | Limitations |
| :--- | :--- |
| The primary advantage is the simplicity of responsibility as the factory approach provides a clear boundary around object creation, where the request-management component does not need to know the construction details of every request type. | The OCP problem is not necessarily solved in the factory based approach. A poorly designed system would just be moving the original conditional chain to a new class, here the conditional decision logic still has to be modified every time a category is added. Thus the design would just relocate the problem and not solve it. |
| Design becomes easier to understand than introducing separate behavioral abstraction when the main variation is simply which type of request should be constructed. | The factory approach is strongest when the main variation focus is on object construction. Further separation may be required in situations where each category also has independently evolving validation and business rules. |
| Testing object creation becomes more focused as construction logic is isolated from the calling service. |

- **Alternative B: Strategy-Based Category Handlers**  
This approach focuses on encapsulating category-specific creation and validation behavior behind common abstraction, with each category having a handler that contains the rules specific to that category, and a central request-creation process that coordinates the operation rather than containing the validation and creation rules for each category.  
| Advantages | Limitations |
| :--- | :--- |
| The strongest advantage is the improved extensibility where a new category can potentially be implemented by adding another handler rather than adding another branch to an already existing creation method. | The main limitation is the additional abstraction, where instead of one creation routine the system could require a handler interface, multiple concrete handlers, a mechanism for selecting the correct handler, and additional classes and tests. Which will make the design more difficult to understand |
| Category-specific behavior is also more cohesive because the validation and creation rules for a category are located together. | There is also an important empirical reason not to assume that Strategy is automatically better. Silva (2021) conducted a quasi-experiment examining the impact of the Strategy Pattern on maintainability. Their findings indicate that the introduction of Strategy can itself affect maintenance performance, reinforcing the need to consider whether the additional abstraction is justified in the particular context. |
| Testing can become more focused, as a test can focus on a specific category handler rather than executing a large central creation routine containing rules for every category. | 
| There is also more support for categories to evolve differently. If one category develops significantly more complex validation rules than another, the complexity will remain in the relevant handler instead of increasing the complexity of every request-creation operation. | 

**Comparison of the Alternatives**
| Criterion | Factory | Strategy |
| :--- | :--- | :--- |
| Primary purpose | Encapsulate object creation | Encapsulate varying behavior |
| Best fit | Different request types require different construction | Categories have different validation/business behavior |
| Creation responsibility | Strong | Moderate/Strong |
| Category-specific validation | Possible, but may require separate validators | Strong |
| OCP support | Depends on implementation | Strong |
| Initial simplicity | High | Low |
| Additional abstractions | Moderate | High |
| Test Isolation | Good | Good |
| Risk of over-engineering | Low | High |
| Best Use | Categories represent different request objects | Categories have independently evolving rules |

### Recommendation feeding M2
**Research**  
Design patterns should be selected according to the actual characteristics of the design problem rather than because the pattern is a recognized solution.Naghdipour (2023) conducted a systematic literature review of approaches to software design-pattern selection and identified pattern selection as a non-trivial decision problem. There is also evidence that pattern structure and implementation can influence software quality, meaning that the use of a pattern alone is not evidence of improved maintainability (Wedyan; 2020). This supports evaluating candidate solutions against the characteristics of the particular design problem rather than selecting a pattern simply because it is available.  

- **Problem 1**  
**Recommendation:**  
A State-oriented design should be considered in the situation where service-request operations contain substantial state-dependant behavior and the lifecycle is likely to evolve.
A Centralized conditional approach should be considered in the situation where there are a small number of stable states and straightforward transition rules.  

- **Problem 2**  
**Recommendation**  
A strategy-based approach is the recommended in the event that the categories are determined to have substantially different and evolving validation or business rules.
In the scenario where the categories primarily represent different request types with different construction requirements, a factory-based approach is recommended.

---
## Task 2


---
## Task 3


---
## Task 4


---
## Task 5
| A2 finding | Evidence/alternative considered | Recommendation | Project decision it should inform | Expected PED/ADR/RTM/application evidence |
| :--- | :--- | :--- | :--- | :--- |
| Design problem 1 | 
| Design problem 2 | 
| Persistence/Data | 
| Integration/API | 
| SCM/CI | 
---
## References
Almadi, S.H., Hooshyar, D. and Ahmad, R.B., 2021. Bad smells of gang of four design patterns: a decade systematic literature review. Sustainability, 13(18), p.10256.
Naghdipour, A., Hasheminejad, S.M.H. and Barmaki, R.L., 2023. Software design pattern selection approaches: a systematic literature review. Software: Practice and Experience, 53(4), pp.1091-1122.
Nikitin, D., 2023. Specification formalization of state charts for complex system management. Bulletin of National Technical University" KhPI". Series: System analysis, control and information technologies, (1 (9)), pp.104-109.
Plösch, R., Bräuer, J., Körner, C. and Saft, M., 2016. Measuring, assessing and improving software quality based on object-oriented design principles. Open Computer Science, 6(1), pp.187-207.
Silva, G., Andrade, V., Ré, R. and Meneses, R., 2021, September. A quasi-experiment to investigating the impact of the strategy design pattern on maintainability. In Proceedings of the XXXV Brazilian Symposium on Software Engineering (pp. 105-114).
Stevenson, J. and Wood, M., 2018. Recognizing object-oriented software design quality: a practitioner-based questionnaire survey. Software Quality Journal, 26(2), pp.321-365.
Tiwari, S. and Rathore, S.S., 2018, February. Coupling and cohesion metrics for object-oriented software: A systematic mapping study. In Proceedings of the 11th Innovations in Software Engineering Conference (pp. 1-11).
Wedyan, F. and Abufakher, S., 2020. Impact of design patterns on software quality: a systematic literature review. IET Software, 14(1), pp.1-17.