namespace NGraphQL.Model.Request {

  public class MappedDirective {
    public DirectiveDef Def;
    public InputValueEvaluator ArgsEvaluator;
    public override string ToString() => Def.Name;
  }

}
