import { cn } from "cn"
import { Button } from "@workspace/ui/components/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@workspace/ui/components/card"
import {
  Field,
  FieldDescription,
  FieldGroup,
  FieldLabel,
} from "@workspace/ui/components/field"
import { Input } from "@workspace/ui/components/input"
import { object, string, type InferType, ref } from 'yup';
import { useSubmit, useActionData, useNavigation } from 'react-router'; 
import { useForm } from "react-hook-form"
import { yupResolver } from "@hookform/resolvers/yup/src/index.js"
import { Link } from "react-router";

const signupSchema = object({
  firstName: string().required("First Name is required"),
  lastName: string().required("Last Name is required"),
  username: string().email("Invalid email").required("Email is required"),
  password: string().min(8, "Password must be at least 8 characters").required("Password is required"),
  confirmPassword: string()
    .oneOf([ref("password")], "Passwords must match")
    .required("Confirm Password is required"),
});

export type SignupFormValues = InferType<typeof signupSchema>;

export function SignupForm({
  className,
  ...props
}: React.ComponentProps<"div">) {
  const submit = useSubmit(); // React Router's submit imperative hook
  const serverErrors = useActionData(); // Catches validation errors returned from the server
  const navigation = useNavigation();

  const { register, handleSubmit, formState: { errors } } = useForm({
    resolver: yupResolver(signupSchema),
    mode: 'onTouched',
  });
  
  // 2. React Hook Form validates on the client first
  const onSubmit = (data: SignupFormValues) => {
    // If valid, submit directly to React Router's server action
    submit(data, { method: 'post', action: '/login' });
  };

  return (
    <div className={cn("flex flex-col gap-6", className)} {...props}>
      <Card>
        <CardHeader className="text-center">
          <CardTitle className="text-xl">Create your account</CardTitle>
          <CardDescription>
            Enter your email below to create your account.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)}>
            <FieldGroup>
              <Field>
                <FieldLabel htmlFor="firstName">First Name</FieldLabel>
                <Input id="firstName" type="text" placeholder="John" required {...register("firstName")} />
                {(errors.firstName || serverErrors?.fieldErrors?.firstName) && (
                  <p style={{ color: 'red' }}>{errors.firstName?.message || serverErrors.fieldErrors.firstName}</p>
                )}
              </Field>
              <Field>
                <FieldLabel htmlFor="lastName">Last Name</FieldLabel>
                <Input id="lastName" type="text" placeholder="Doe" required {...register("lastName")} />
                {(errors.lastName || serverErrors?.fieldErrors?.lastName) && (
                  <p style={{ color: 'red' }}>{errors.lastName?.message || serverErrors.fieldErrors.lastName}</p>
                )}
              </Field>
              <Field>
                <FieldLabel htmlFor="username">Email</FieldLabel>
                <Input
                  id="username"
                  type="email"
                  placeholder="m@example.com"
                  required
                  {...register("username")}
                />
                  {(errors.username || serverErrors?.fieldErrors?.username) && (
                    <p style={{ color: 'red' }}>{errors.username?.message || serverErrors.fieldErrors.username}</p>
                  )}
              </Field>
              <Field>
                <Field className="grid grid-cols-2 gap-4">
                  <Field>
                    <FieldLabel htmlFor="password">Password</FieldLabel>
                    <Input id="password" type="password" required {...register("password")} />
                   {(errors.password || serverErrors?.fieldErrors?.password) && (
                      <p style={{ color: 'red' }}>{errors.password?.message || serverErrors.fieldErrors.password}</p>
                    )}
                  </Field>
                  <Field>
                    <FieldLabel htmlFor="confirm-password">
                      Confirm Password
                    </FieldLabel>
                    <Input id="confirm-password" type="password" required {...register("confirmPassword")} />
                    {(errors.confirmPassword || serverErrors?.fieldErrors?.confirmPassword) && (
                      <p style={{ color: 'red' }}>{errors.confirmPassword?.message || serverErrors.fieldErrors.confirmPassword}</p>
                    )}
                  </Field>
                </Field>
                <FieldDescription>
                  Must be at least 8 characters long.
                </FieldDescription>
              </Field>
              <Field>
                <Button type="submit">Create Account</Button>
                <FieldDescription className="text-center">
                  Already have an account? <Link to="/login" className="text-primary">Sign in</Link>
                </FieldDescription>
              </Field>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>
      <FieldDescription className="px-6 text-center">
        By clicking continue, you agree to our <a href="#">Terms of Service</a>{" "}
        and <a href="#">Privacy Policy</a>.
      </FieldDescription>
    </div>
  )
}
